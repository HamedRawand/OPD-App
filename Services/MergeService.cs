using System.IO;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OPDClinic.Data;
using OPDClinic.Models;
using Serilog;

namespace OPDClinic.Services;

/// <summary>
/// Merges data from a backup file (.zip or .rxb) into the current master database.
///
/// Rules:
///   • Patients are always imported as new rows — no deduplication.
///     Every imported patient is tagged with <paramref name="clinicName"/> in SourceClinic.
///   • Reference data (Physicians, LabTests, MedicineLists) is deduplicated by name;
///     if a matching name already exists in master its ID is reused.
///   • All IDs are remapped so there are no primary-key conflicts.
///   • Both the legacy single-visit schema (no Visits table) and the current
///     multi-visit schema are handled.
/// </summary>
public static class MergeService
{
    public static async Task<MergeResult> MergeFromBackupAsync(
        IDbContextFactory<AppDbContext> masterFactory,
        string backupPath,
        string clinicName,
        string? password = null,
        IProgress<string>? progress = null)
    {
        // Extract / decrypt to a temp SQLite file
        var tempPath = BackupService.ExtractToTempFile(backupPath, password);
        try
        {
            return await Task.Run(() => DoMerge(masterFactory, tempPath, clinicName, progress));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            try { File.Delete(tempPath); } catch { /* best effort */ }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────

    private static MergeResult DoMerge(
        IDbContextFactory<AppDbContext> masterFactory,
        string sourcePath,
        string clinicName,
        IProgress<string>? progress)
    {
        var result = new MergeResult { ClinicName = clinicName };

        using var src = new SqliteConnection($"Data Source={sourcePath};Mode=ReadOnly");
        src.Open();

        // Detect whether the backup uses the new multi-visit schema
        bool hasVisits = TableExists(src, "Visits");

        using var db = masterFactory.CreateDbContext();
        using var tx = db.Database.BeginTransaction();

        try
        {
            progress?.Report("Merging physicians…");
            var physicianMap = MergePhysicians(src, db, result);

            progress?.Report("Merging lab tests…");
            var labTestMap = MergeLabTests(src, db, result);

            progress?.Report("Merging medicine catalog…");
            MergeMedicines(src, db, result);

            progress?.Report("Importing patients…");
            var patientMap = ImportPatients(src, db, clinicName, result);

            progress?.Report("Importing visits…");
            var visitMap = ImportVisits(src, db, patientMap, physicianMap, hasVisits, result);

            progress?.Report("Importing prescriptions…");
            ImportMedicineUsages(src, db, visitMap, result);

            progress?.Report("Importing lab results…");
            ImportPatientLabTests(src, db, visitMap, labTestMap, result);

            tx.Commit();

            AuditService.Log("BackupMerged",
                details: $"{clinicName}: {result.PatientsImported}p / {result.VisitsImported}v");
            Log.Information("Merge complete — clinic={Clinic} patients={P} visits={V}",
                clinicName, result.PatientsImported, result.VisitsImported);

            progress?.Report("Done.");
        }
        catch (Exception ex)
        {
            tx.Rollback();
            Log.Error(ex, "Merge failed for clinic={Clinic}", clinicName);
            throw;
        }

        return result;
    }

    // ── Reference data ────────────────────────────────────────────────────────

    private static Dictionary<int, int> MergePhysicians(
        SqliteConnection src, AppDbContext db, MergeResult result)
    {
        var map = new Dictionary<int, int>();
        if (!TableExists(src, "Physicians")) return map;

        // Index existing master physicians by NameEng (case-insensitive)
        var byName = db.Physicians
            .Select(p => new { p.Id, p.NameEng })
            .ToList()
            .Where(p => p.NameEng != null)
            .ToDictionary(p => p.NameEng!.Trim().ToLowerInvariant(), p => p.Id);

        using var cmd = src.CreateCommand();
        cmd.CommandText =
            "SELECT \"Id\", \"NameEng\", \"NameDari\", \"SpecialityEng\", \"SpecialityDari\", " +
            "\"OtherSpecialityEng\", \"OtherSpecialityDari\", " +
            "\"ContactNumber\", \"WhatsAppNumber\", \"ReceptionContactNumber\", \"Address\" " +
            "FROM Physicians";
        using var r = cmd.ExecuteReader();

        while (r.Read())
        {
            var srcId   = r.GetInt32(0);
            var nameEng = r.IsDBNull(1) ? null : r.GetString(1);
            var key     = nameEng?.Trim().ToLowerInvariant() ?? "";

            if (!string.IsNullOrEmpty(key) && byName.TryGetValue(key, out var existing))
            {
                map[srcId] = existing;   // reuse master's physician
            }
            else
            {
                var p = new Physician
                {
                    NameEng                = nameEng,
                    NameDari               = r.IsDBNull(2)  ? null : r.GetString(2),
                    SpecialityEng          = r.IsDBNull(3)  ? null : r.GetString(3),
                    SpecialityDari         = r.IsDBNull(4)  ? null : r.GetString(4),
                    OtherSpecialityEng     = r.IsDBNull(5)  ? null : r.GetString(5),
                    OtherSpecialityDari    = r.IsDBNull(6)  ? null : r.GetString(6),
                    ContactNumber          = r.IsDBNull(7)  ? null : r.GetString(7),
                    WhatsAppNumber         = r.IsDBNull(8)  ? null : r.GetString(8),
                    ReceptionContactNumber = r.IsDBNull(9)  ? null : r.GetString(9),
                    Address                = r.IsDBNull(10) ? null : r.GetString(10),
                };
                db.Physicians.Add(p);
                db.SaveChanges();
                map[srcId] = p.Id;
                if (!string.IsNullOrEmpty(key)) byName[key] = p.Id;
                result.PhysiciansAdded++;
            }
        }
        return map;
    }

    private static Dictionary<int, int> MergeLabTests(
        SqliteConnection src, AppDbContext db, MergeResult result)
    {
        var map = new Dictionary<int, int>();
        if (!TableExists(src, "LabTests")) return map;

        var byName = db.LabTests
            .Select(t => new { t.Id, t.TestName })
            .ToList()
            .Where(t => t.TestName != null)
            .ToDictionary(t => t.TestName!.Trim().ToLowerInvariant(), t => t.Id);

        using var cmd = src.CreateCommand();
        cmd.CommandText =
            "SELECT \"Id\", \"Category\", \"TestName\", \"Abbreviation\", \"Specimen\", \"Description\" " +
            "FROM LabTests";
        using var r = cmd.ExecuteReader();

        while (r.Read())
        {
            var srcId    = r.GetInt32(0);
            var testName = r.IsDBNull(2) ? null : r.GetString(2);
            var key      = testName?.Trim().ToLowerInvariant() ?? "";

            if (!string.IsNullOrEmpty(key) && byName.TryGetValue(key, out var existing))
            {
                map[srcId] = existing;
            }
            else
            {
                var t = new LabTest
                {
                    Category     = r.IsDBNull(1) ? null : r.GetString(1),
                    TestName     = testName,
                    Abbreviation = r.IsDBNull(3) ? null : r.GetString(3),
                    Specimen     = r.IsDBNull(4) ? null : r.GetString(4),
                    Description  = r.IsDBNull(5) ? null : r.GetString(5),
                };
                db.LabTests.Add(t);
                db.SaveChanges();
                map[srcId] = t.Id;
                if (!string.IsNullOrEmpty(key)) byName[key] = t.Id;
                result.LabTestsAdded++;
            }
        }
        return map;
    }

    private static void MergeMedicines(
        SqliteConnection src, AppDbContext db, MergeResult result)
    {
        if (!TableExists(src, "MedicineLists")) return;

        var existing = db.MedicineLists
            .Select(m => m.MedicineName)
            .Where(n => n != null)
            .Select(n => n!.Trim().ToLowerInvariant())
            .ToHashSet();

        using var cmd = src.CreateCommand();
        cmd.CommandText =
            "SELECT \"MedicineName\", \"GenericName\", \"Category\", \"Type\", \"Strength\", \"Note\" " +
            "FROM MedicineLists";
        using var r = cmd.ExecuteReader();

        var batch = new List<MedicineList>();
        while (r.Read())
        {
            var name = r.IsDBNull(0) ? null : r.GetString(0);
            var key  = name?.Trim().ToLowerInvariant() ?? "";
            if (!string.IsNullOrEmpty(key) && existing.Contains(key)) continue;

            batch.Add(new MedicineList
            {
                MedicineName = name,
                GenericName  = r.IsDBNull(1) ? null : r.GetString(1),
                Category     = r.IsDBNull(2) ? null : r.GetString(2),
                Type         = r.IsDBNull(3) ? null : r.GetString(3),
                Strength     = r.IsDBNull(4) ? null : r.GetString(4),
                Note         = r.IsDBNull(5) ? null : r.GetString(5),
            });
            if (!string.IsNullOrEmpty(key)) existing.Add(key);
            result.MedicinesAdded++;
        }
        if (batch.Count > 0) { db.MedicineLists.AddRange(batch); db.SaveChanges(); }
    }

    // ── Patients ──────────────────────────────────────────────────────────────

    private static Dictionary<int, int> ImportPatients(
        SqliteConnection src, AppDbContext db, string clinicName, MergeResult result)
    {
        var map  = new Dictionary<int, int>();
        var cols = GetColumns(src, "Patients");

        // CreatedAt was called OpdDate in the legacy schema
        var createdAtExpr = cols.Contains("CreatedAt") ? "\"CreatedAt\""
                          : cols.Contains("OpdDate")   ? "\"OpdDate\""
                          : "NULL";

        using var cmd = src.CreateCommand();
        cmd.CommandText =
            $"SELECT \"Id\", \"PatientName\", \"Sex\", \"PatientNumber\", {createdAtExpr} " +
            "FROM Patients";
        using var r = cmd.ExecuteReader();

        while (r.Read())
        {
            var srcId = r.GetInt32(0);
            var p = new Patient
            {
                PatientName  = r.IsDBNull(1) ? null : r.GetString(1),
                Sex          = r.IsDBNull(2) ? null : r.GetString(2),
                PhoneNumber  = r.IsDBNull(3) ? null : r.GetString(3),
                CreatedAt    = ReadDateTime(r, 4),
                SourceClinic = clinicName,
            };
            db.Patients.Add(p);
            db.SaveChanges();

            // Regenerate PatientCode using the master's new auto-increment ID
            p.PatientCode = $"P-{p.Id:D5}";
            db.SaveChanges();

            map[srcId] = p.Id;
            result.PatientsImported++;
        }
        return map;
    }

    // ── Visits ────────────────────────────────────────────────────────────────

    private static Dictionary<int, int> ImportVisits(
        SqliteConnection src, AppDbContext db,
        Dictionary<int, int> patientMap,
        Dictionary<int, int> physicianMap,
        bool hasVisits, MergeResult result)
    {
        var map = new Dictionary<int, int>();

        if (!hasVisits)
        {
            // Legacy schema: one visit synthesised per patient row
            ImportVisitsFromOldSchema(src, db, patientMap, physicianMap, map, result);
        }
        else
        {
            ImportVisitsFromNewSchema(src, db, patientMap, physicianMap, map, result);
        }
        return map;
    }

    private static void ImportVisitsFromOldSchema(
        SqliteConnection src, AppDbContext db,
        Dictionary<int, int> patientMap, Dictionary<int, int> physicianMap,
        Dictionary<int, int> visitMap, MergeResult result)
    {
        using var cmd = src.CreateCommand();
        cmd.CommandText =
            "SELECT \"Id\", \"PhysicianId\", \"OpdDate\", \"HijriDate\", \"Age\", " +
            "\"BP\", \"HR\", \"PR\", \"RR\", \"BT\", \"BW\", " +
            "\"ClinicalFindings\", \"Diagnosis\", \"Note\", \"LastUpdated\" " +
            "FROM Patients";
        using var r = cmd.ExecuteReader();

        while (r.Read())
        {
            var srcPatientId = r.GetInt32(0);
            if (!patientMap.TryGetValue(srcPatientId, out var masterPatientId)) continue;

            int? masterPhysicianId = MapId(physicianMap, r.IsDBNull(1) ? (int?)null : r.GetInt32(1));

            var v = new Visit
            {
                PatientId        = masterPatientId,
                PhysicianId      = masterPhysicianId,
                OpdDate          = ReadDateTime(r, 2),
                HijriDate        = r.IsDBNull(3)  ? null : r.GetString(3),
                Age              = r.IsDBNull(4)  ? null : r.GetInt32(4),
                BP               = r.IsDBNull(5)  ? null : r.GetString(5),
                HR               = r.IsDBNull(6)  ? null : r.GetString(6),
                PR               = r.IsDBNull(7)  ? null : r.GetString(7),
                RR               = r.IsDBNull(8)  ? null : r.GetString(8),
                BT               = r.IsDBNull(9)  ? null : r.GetString(9),
                BW               = r.IsDBNull(10) ? null : r.GetString(10),
                ClinicalFindings = r.IsDBNull(11) ? null : r.GetString(11),
                Diagnosis        = r.IsDBNull(12) ? null : r.GetString(12),
                Note             = r.IsDBNull(13) ? null : r.GetString(13),
                LastUpdated      = ReadDateTime(r, 14),
            };
            db.Visits.Add(v);
            db.SaveChanges();
            // Old schema: MedicineUsages reference PatientId, so map srcPatientId → new Visit Id
            visitMap[srcPatientId] = v.Id;
            result.VisitsImported++;
        }
    }

    private static void ImportVisitsFromNewSchema(
        SqliteConnection src, AppDbContext db,
        Dictionary<int, int> patientMap, Dictionary<int, int> physicianMap,
        Dictionary<int, int> visitMap, MergeResult result)
    {
        using var cmd = src.CreateCommand();
        cmd.CommandText =
            "SELECT \"Id\", \"PatientId\", \"PhysicianId\", " +
            "\"OpdDate\", \"HijriDate\", \"Age\", " +
            "\"BP\", \"HR\", \"PR\", \"RR\", \"BT\", \"BW\", " +
            "\"ClinicalFindings\", \"Diagnosis\", \"FooterNote\", \"Note\", \"LastUpdated\" " +
            "FROM Visits";
        using var r = cmd.ExecuteReader();

        while (r.Read())
        {
            var srcVisitId   = r.GetInt32(0);
            var srcPatientId = r.GetInt32(1);
            if (!patientMap.TryGetValue(srcPatientId, out var masterPatientId)) continue;

            int? masterPhysicianId = MapId(physicianMap, r.IsDBNull(2) ? (int?)null : r.GetInt32(2));

            var v = new Visit
            {
                PatientId        = masterPatientId,
                PhysicianId      = masterPhysicianId,
                OpdDate          = ReadDateTime(r, 3),
                HijriDate        = r.IsDBNull(4)  ? null : r.GetString(4),
                Age              = r.IsDBNull(5)  ? null : r.GetInt32(5),
                BP               = r.IsDBNull(6)  ? null : r.GetString(6),
                HR               = r.IsDBNull(7)  ? null : r.GetString(7),
                PR               = r.IsDBNull(8)  ? null : r.GetString(8),
                RR               = r.IsDBNull(9)  ? null : r.GetString(9),
                BT               = r.IsDBNull(10) ? null : r.GetString(10),
                BW               = r.IsDBNull(11) ? null : r.GetString(11),
                ClinicalFindings = r.IsDBNull(12) ? null : r.GetString(12),
                Diagnosis        = r.IsDBNull(13) ? null : r.GetString(13),
                FooterNote       = r.IsDBNull(14) ? null : r.GetString(14),
                Note             = r.IsDBNull(15) ? null : r.GetString(15),
                LastUpdated      = ReadDateTime(r, 16),
            };
            db.Visits.Add(v);
            db.SaveChanges();
            visitMap[srcVisitId] = v.Id;
            result.VisitsImported++;
        }
    }

    // ── Medicine usages ───────────────────────────────────────────────────────

    private static void ImportMedicineUsages(
        SqliteConnection src, AppDbContext db,
        Dictionary<int, int> visitMap, MergeResult result)
    {
        if (!TableExists(src, "MedicineUsages")) return;

        // Support both old schema (PatientId) and new schema (VisitId)
        var cols  = GetColumns(src, "MedicineUsages");
        var idCol = cols.Contains("VisitId") ? "\"VisitId\"" : "\"PatientId\"";

        using var cmd = src.CreateCommand();
        cmd.CommandText =
            $"SELECT {idCol}, \"LineNumber\", \"Type\", \"Prescription\", " +
            "\"Strength\", \"Qty\", \"Usage\", \"RouteName\", \"Note\" " +
            "FROM MedicineUsages";
        using var r = cmd.ExecuteReader();

        var batch = new List<MedicineUsage>();
        while (r.Read())
        {
            var srcId = r.GetInt32(0);
            if (!visitMap.TryGetValue(srcId, out var masterVisitId)) continue;

            batch.Add(new MedicineUsage
            {
                VisitId      = masterVisitId,
                LineNumber   = r.IsDBNull(1) ? 0 : r.GetInt32(1),
                Type         = r.IsDBNull(2) ? null : r.GetString(2),
                Prescription = r.IsDBNull(3) ? null : r.GetString(3),
                Strength     = r.IsDBNull(4) ? null : r.GetString(4),
                Qty          = r.IsDBNull(5) ? null : (int?)r.GetInt32(5),
                Usage        = r.IsDBNull(6) ? null : r.GetString(6),
                RouteName    = r.IsDBNull(7) ? null : r.GetString(7),
                Note         = r.IsDBNull(8) ? null : r.GetString(8),
            });
            result.PrescriptionLinesImported++;
        }
        if (batch.Count > 0) { db.MedicineUsages.AddRange(batch); db.SaveChanges(); }
    }

    // ── Lab tests ─────────────────────────────────────────────────────────────

    private static void ImportPatientLabTests(
        SqliteConnection src, AppDbContext db,
        Dictionary<int, int> visitMap,
        Dictionary<int, int> labTestMap, MergeResult result)
    {
        if (!TableExists(src, "PatientLabTests")) return;

        var cols  = GetColumns(src, "PatientLabTests");
        var idCol = cols.Contains("VisitId") ? "\"VisitId\"" : "\"PatientId\"";

        using var cmd = src.CreateCommand();
        cmd.CommandText = $"SELECT {idCol}, \"LabTestId\" FROM PatientLabTests";
        using var r = cmd.ExecuteReader();

        var batch = new List<PatientLabTest>();
        while (r.Read())
        {
            var srcVisitId   = r.GetInt32(0);
            var srcLabTestId = r.GetInt32(1);
            if (!visitMap.TryGetValue(srcVisitId,   out var masterVisitId))   continue;
            if (!labTestMap.TryGetValue(srcLabTestId, out var masterLabTestId)) continue;

            batch.Add(new PatientLabTest { VisitId = masterVisitId, LabTestId = masterLabTestId });
            result.LabResultsImported++;
        }
        if (batch.Count > 0) { db.PatientLabTests.AddRange(batch); db.SaveChanges(); }
    }

    // ── SQLite helpers ────────────────────────────────────────────────────────

    private static bool TableExists(SqliteConnection conn, string name)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@n";
        cmd.Parameters.AddWithValue("@n", name);
        return (long)cmd.ExecuteScalar()! > 0;
    }

    private static HashSet<string> GetColumns(SqliteConnection conn, string table)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info(\"{table}\")";
        using var r = cmd.ExecuteReader();
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (r.Read()) set.Add(r.GetString(1));   // col 1 = name
        return set;
    }

    private static DateTime? ReadDateTime(SqliteDataReader r, int ordinal)
    {
        if (r.IsDBNull(ordinal)) return null;
        try   { return r.GetDateTime(ordinal); }
        catch { try { return DateTime.TryParse(r.GetString(ordinal), out var dt) ? dt : null; }
                catch { return null; } }
    }

    private static int? MapId(Dictionary<int, int> map, int? srcId)
    {
        if (srcId is null) return null;
        return map.TryGetValue(srcId.Value, out var masterId) ? masterId : null;
    }
}

// ─────────────────────────────────────────────────────────────────────────────

public class MergeResult
{
    public string ClinicName                 { get; init; } = "";
    public int    PatientsImported           { get; set; }
    public int    VisitsImported             { get; set; }
    public int    PrescriptionLinesImported  { get; set; }
    public int    LabResultsImported         { get; set; }
    public int    PhysiciansAdded            { get; set; }
    public int    MedicinesAdded             { get; set; }
    public int    LabTestsAdded              { get; set; }

    public string Summary
    {
        get
        {
            var clinical = $"{PatientsImported} patients · {VisitsImported} visits";
            var catalog  = new List<string>();
            if (PhysiciansAdded > 0) catalog.Add($"+{PhysiciansAdded} physician{(PhysiciansAdded != 1 ? "s" : "")}");
            if (MedicinesAdded  > 0) catalog.Add($"+{MedicinesAdded} medicine{(MedicinesAdded  != 1 ? "s" : "")}");
            if (LabTestsAdded   > 0) catalog.Add($"+{LabTestsAdded} lab test{(LabTestsAdded    != 1 ? "s" : "")}");
            return catalog.Count > 0
                ? $"{clinical} imported.  Catalog: {string.Join(", ", catalog)}."
                : $"{clinical} imported.";
        }
    }
}
