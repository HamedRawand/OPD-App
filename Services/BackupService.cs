using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using Microsoft.Data.Sqlite;

namespace OPDClinic.Services;

/// <summary>
/// Backup file formats supported:
///   .zip  — legacy unencrypted zip (clinic.db inside)
///   .rxb  — AES-256-CBC encrypted Rx Writer Backup (magic + salt + IV + ciphertext)
/// </summary>
public class BackupService
{
    public static string DefaultBackupFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                     "OPDClinic", "Backups");

    // File magic for .rxb — 4 bytes: "RXW1"
    private static readonly byte[] RxbMagic = [0x52, 0x58, 0x57, 0x31];

    // ── Create ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates an unencrypted timestamped zip backup.
    /// Returns the full path of the created zip file.
    /// </summary>
    public static string CreateBackup(string targetFolder)
        => CreateBackup(targetFolder, password: null);

    /// <summary>
    /// Creates a timestamped backup.
    /// If <paramref name="password"/> is null/empty → unencrypted .zip (legacy).
    /// If <paramref name="password"/> is set        → AES-256 encrypted .rxb file.
    /// Returns the full path of the created file.
    /// </summary>
    public static string CreateBackup(string targetFolder, string? password)
    {
        Directory.CreateDirectory(targetFolder);
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        var encrypted = !string.IsNullOrWhiteSpace(password);
        var ext       = encrypted ? "rxb" : "zip";
        var filePath  = Path.Combine(targetFolder, $"OPDClinic_backup_{timestamp}.{ext}");

        // Copy live DB to a temp file via SQLite online backup API (WAL-safe, no lock needed)
        var tempPath = Path.GetTempFileName();
        try
        {
            using (var src  = new SqliteConnection($"Data Source={App.DbPath}"))
            using (var dest = new SqliteConnection($"Data Source={tempPath}"))
            {
                src.Open();
                dest.Open();
                src.BackupDatabase(dest);
            }
            SqliteConnection.ClearAllPools();

            if (encrypted)
                WriteEncryptedRxb(tempPath, filePath, password!);
            else
                WriteZip(tempPath, filePath);
        }
        finally
        {
            try { File.Delete(tempPath); } catch { /* best-effort */ }
        }

        return filePath;
    }

    private static void WriteZip(string dbPath, string zipPath)
    {
        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        zip.CreateEntryFromFile(dbPath, "clinic.db", CompressionLevel.Optimal);
    }

    private static void WriteEncryptedRxb(string dbPath, string rxbPath, string password)
    {
        var dbBytes = File.ReadAllBytes(dbPath);

        // Derive 256-bit key from password using PBKDF2/SHA-256 with a fresh random salt
        var salt = RandomNumberGenerator.GetBytes(16);
        var iv   = RandomNumberGenerator.GetBytes(16);
        var key  = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);

        using var aes       = Aes.Create();
        aes.Key             = key;
        aes.IV              = iv;
        aes.Mode            = CipherMode.CBC;
        aes.Padding         = PaddingMode.PKCS7;
        using var encryptor = aes.CreateEncryptor();

        using var ms = new MemoryStream();
        ms.Write(RxbMagic);          // 4-byte magic
        ms.Write(salt);              // 16-byte salt
        ms.Write(iv);                // 16-byte IV

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write, leaveOpen: true))
            cs.Write(dbBytes, 0, dbBytes.Length);

        File.WriteAllBytes(rxbPath, ms.ToArray());
    }

    // ── Restore ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Detects format and restores the backup (password required for .rxb files, ignored for .zip).
    /// Disposes the current DbContext; caller must restart the app after this returns.
    /// </summary>
    public static void RestoreBackup(string backupPath, string? password = null)
    {
        var ext = Path.GetExtension(backupPath).ToLowerInvariant();

        byte[] restoredDb;
        if (ext == ".rxb")
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException(
                    "This backup is encrypted. Please enter the backup password to restore.");
            restoredDb = DecryptRxb(backupPath, password);
        }
        else
        {
            restoredDb = ExtractZip(backupPath);
        }

        // Flush all pooled SQLite connections before replacing the file
        SqliteConnection.ClearAllPools();

        var dbPath  = App.DbPath;
        var bakPath = dbPath + ".bak";

        File.Copy(dbPath, bakPath, overwrite: true);
        try
        {
            File.WriteAllBytes(dbPath, restoredDb);

            // Verify integrity of the restored database
            using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                using var cmd     = conn.CreateCommand();
                cmd.CommandText   = "PRAGMA integrity_check";
                var result        = cmd.ExecuteScalar()?.ToString();
                if (result != "ok")
                    throw new InvalidOperationException(
                        $"Restored database failed integrity check: {result}");
            }
            SqliteConnection.ClearAllPools();

            File.Delete(bakPath);
        }
        catch
        {
            // Roll back to what we had
            try { File.Copy(bakPath, dbPath, overwrite: true); } catch { /* best effort */ }
            try { File.Delete(bakPath); } catch { /* best effort */ }
            throw;
        }
    }

    private static byte[] ExtractZip(string zipPath)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        var entry = zip.GetEntry("clinic.db")
            ?? throw new InvalidOperationException(
                "The selected zip does not contain a valid OPDClinic backup (clinic.db not found).");
        using var stream = entry.Open();
        using var ms     = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    private static byte[] DecryptRxb(string rxbPath, string password)
    {
        var fileBytes = File.ReadAllBytes(rxbPath);

        // Validate magic
        if (fileBytes.Length < 36 || // 4 magic + 16 salt + 16 iv minimum
            fileBytes[0] != RxbMagic[0] || fileBytes[1] != RxbMagic[1] ||
            fileBytes[2] != RxbMagic[2] || fileBytes[3] != RxbMagic[3])
            throw new InvalidOperationException(
                "The selected file is not a valid Rx Writer encrypted backup (.rxb).");

        var salt      = fileBytes[4..20];
        var iv        = fileBytes[20..36];
        var ciphertext = fileBytes[36..];

        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, 100_000, HashAlgorithmName.SHA256, 32);

        using var aes       = Aes.Create();
        aes.Key             = key;
        aes.IV              = iv;
        aes.Mode            = CipherMode.CBC;
        aes.Padding         = PaddingMode.PKCS7;
        using var decryptor = aes.CreateDecryptor();

        try
        {
            using var ms  = new MemoryStream(ciphertext);
            using var cs  = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var out_ = new MemoryStream();
            cs.CopyTo(out_);
            return out_.ToArray();
        }
        catch (CryptographicException)
        {
            throw new InvalidOperationException(
                "Incorrect password — could not decrypt the backup file.");
        }
    }

    // ── List / Delete ─────────────────────────────────────────────────────────

    /// <summary>Returns all backup files in <paramref name="folder"/> (.zip + .rxb), newest first.</summary>
    public static List<BackupFile> ListBackups(string folder)
    {
        if (!Directory.Exists(folder)) return [];

        var zips = Directory.GetFiles(folder, "OPDClinic_backup_*.zip");
        var rxbs = Directory.GetFiles(folder, "OPDClinic_backup_*.rxb");

        return zips.Concat(rxbs)
            .Select(p => new BackupFile(p))
            .OrderByDescending(b => b.CreatedAt)
            .ToList();
    }

    /// <summary>Restarts the current process cleanly.</summary>
    public static void RestartApp()
    {
        var exe = Environment.ProcessPath
               ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;

        if (string.IsNullOrEmpty(exe))
            throw new InvalidOperationException("Cannot determine the application executable path.");

        System.Diagnostics.Process.Start(exe);
        System.Windows.Application.Current.Shutdown();
    }
}

public class BackupFile(string path)
{
    public string   Path        { get; } = path;
    public string   FileName    { get; } = System.IO.Path.GetFileName(path);
    public long     SizeBytes   { get; } = new System.IO.FileInfo(path).Length;
    public bool     IsEncrypted { get; } = System.IO.Path.GetExtension(path)
                                               .Equals(".rxb", StringComparison.OrdinalIgnoreCase);
    public string   SizeText    => SizeBytes < 1024 * 1024
        ? $"{SizeBytes / 1024.0:F1} KB"
        : $"{SizeBytes / (1024.0 * 1024):F2} MB";
    public DateTime CreatedAt   { get; } = new System.IO.FileInfo(path).LastWriteTime;
    public string   CreatedText => CreatedAt.ToString("yyyy-MM-dd  HH:mm:ss");
}
