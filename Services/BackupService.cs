using System.IO;
using System.IO.Compression;
using Microsoft.Data.Sqlite;

namespace OPDClinic.Services;

public class BackupService
{
    public static string DefaultBackupFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                     "OPDClinic", "Backups");

    /// <summary>
    /// Creates a timestamped zip of clinic.db in <paramref name="targetFolder"/>.
    /// Uses SQLite's online backup API so the database does not need to be closed.
    /// Returns the full path of the created zip file.
    /// </summary>
    public static string CreateBackup(string targetFolder)
    {
        Directory.CreateDirectory(targetFolder);

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var zipPath   = Path.Combine(targetFolder, $"OPDClinic_backup_{timestamp}.zip");

        // SQLite's online backup API copies the DB into a temp file while it is
        // in use — no need to close EF Core's connection or touch the original file.
        var tempPath = Path.GetTempFileName();
        try
        {
            using (var src  = new SqliteConnection($"Data Source={App.DbPath}"))
            using (var dest = new SqliteConnection($"Data Source={tempPath}"))
            {
                src.Open();
                dest.Open();
                src.BackupDatabase(dest);   // atomic, WAL-safe
            }
            // Clear the temp connection from the pool before we read the file
            SqliteConnection.ClearAllPools();

            using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
            zip.CreateEntryFromFile(tempPath, "clinic.db", CompressionLevel.Optimal);
        }
        finally
        {
            try { File.Delete(tempPath); } catch { /* best-effort cleanup */ }
        }

        return zipPath;
    }

    /// <summary>
    /// Extracts clinic.db from <paramref name="zipPath"/> and overwrites the live
    /// database after verifying integrity. Disposes the current DbContext so the
    /// file lock is released. Caller must restart the app after this returns.
    /// </summary>
    public static void RestoreBackup(string zipPath)
    {
        using var zip = ZipFile.OpenRead(zipPath);
        var entry = zip.GetEntry("clinic.db")
            ?? throw new InvalidOperationException(
                "The selected zip does not contain a valid OPDClinic backup (clinic.db not found).");

        // Release EF Core's DbContext and flush ALL pooled SQLite connections
        // so nothing holds a lock on clinic.db before we overwrite it.
        App.Db.Dispose();
        SqliteConnection.ClearAllPools();

        var dbPath  = App.DbPath;
        var bakPath = dbPath + ".bak";

        // Keep a safety copy in case extraction or integrity check fails.
        File.Copy(dbPath, bakPath, overwrite: true);
        try
        {
            entry.ExtractToFile(dbPath, overwrite: true);

            // Verify the restored file is a valid SQLite database.
            using (var conn = new SqliteConnection($"Data Source={dbPath}"))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "PRAGMA integrity_check";
                var result = cmd.ExecuteScalar()?.ToString();
                if (result != "ok")
                    throw new InvalidOperationException(
                        $"Restored database failed integrity check: {result}");
            }
            SqliteConnection.ClearAllPools();   // release integrity-check connection

            File.Delete(bakPath);
        }
        catch
        {
            // Roll back to what we had.
            File.Copy(bakPath, dbPath, overwrite: true);
            File.Delete(bakPath);
            throw;
        }
    }

    /// <summary>Returns all backup zip files in <paramref name="folder"/>, newest first.</summary>
    public static List<BackupFile> ListBackups(string folder)
    {
        if (!Directory.Exists(folder)) return [];

        return Directory
            .GetFiles(folder, "OPDClinic_backup_*.zip")
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
    public string   Path      { get; } = path;
    public string   FileName  { get; } = System.IO.Path.GetFileName(path);
    public long     SizeBytes { get; } = new System.IO.FileInfo(path).Length;
    public string   SizeText  => SizeBytes < 1024 * 1024
        ? $"{SizeBytes / 1024.0:F1} KB"
        : $"{SizeBytes / (1024.0 * 1024):F2} MB";
    public DateTime CreatedAt { get; } = new System.IO.FileInfo(path).LastWriteTime;
    public string   CreatedText => CreatedAt.ToString("yyyy-MM-dd  HH:mm:ss");
}
