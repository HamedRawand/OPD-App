using System.Data.OleDb;
using Microsoft.EntityFrameworkCore;
using OPDClinic.Data;
using OPDClinic.Models;

namespace OPDClinic.Services;

/// <summary>
/// One-time importer: reads the original Access .accdb and populates SQLite.
/// Run from admin settings menu. Safe to call repeatedly — skips tables already populated.
/// </summary>
public class MigrationService(IDbContextFactory<AppDbContext> factory)
{
    public MigrationResult Import(string accdbPath)
    {
        var result = new MigrationResult();

        try
        {
            var connStr = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={accdbPath};Persist Security Info=False;";
            using var conn = new OleDbConnection(connStr);
            conn.Open();

            // One context for the entire import — keeps identity map consistent
            using var db = factory.CreateDbContext();

            result.Physicians        = ImportPhysicians(conn, db);
            result.MedicineForms     = ImportMedicineForms(conn, db);
            result.Routes            = ImportRoutes(conn, db);
            result.Dosages           = ImportDosages(conn, db);
            result.MedicineNotes     = ImportMedicineNotes(conn, db);
            result.PrescriptionNotes = ImportPrescriptionNotes(conn, db);
            result.LabTests          = ImportLabTests(conn, db);
            result.Medicines         = ImportMedicineList(conn, db);
            result.Patients          = ImportPatients(conn, db);
            result.Prescriptions     = ImportMedicineUsage(conn, db);

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
        }

        return result;
    }

    private static int ImportPhysicians(OleDbConnection conn, AppDbContext db)
    {
        if (db.Physicians.Any()) return 0;
        int count = 0;
        using var cmd = new OleDbCommand("SELECT * FROM [Add Physician]", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            db.Physicians.Add(new Physician
            {
                NameEng                = reader["Physician_name_Eng"] as string,
                NameDari               = reader["Physician_name_Dari"] as string,
                SpecialityEng          = reader["Specialities_Eng"] as string,
                SpecialityDari         = reader["Specialities_Dari"] as string,
                OtherSpecialityEng     = reader["Other_specialities_Eng"] as string,
                OtherSpecialityDari    = reader["Other_specialities_Dari"] as string,
                ContactNumber          = reader["Contact_number"] as string,
                WhatsAppNumber         = reader["WhatsApp_number"] as string,
                ReceptionContactNumber = reader["Receiption_contact_number"] as string,
                Address                = reader["Address"] as string,
            });
            count++;
        }
        db.SaveChanges();
        return count;
    }

    private static int ImportMedicineForms(OleDbConnection conn, AppDbContext db)
    {
        if (db.MedicineForms.Any()) return 0;
        int count = 0;
        using var cmd = new OleDbCommand("SELECT * FROM [Medicine Form]", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            db.MedicineForms.Add(new MedicineForm
            {
                Category     = reader["category"] as string,
                FormName     = reader["medicine_form"] as string,
                Abbreviation = reader["Abbreviations"] as string,
                Note         = reader["Note"] as string,
            });
            count++;
        }
        db.SaveChanges();
        return count;
    }

    private static int ImportRoutes(OleDbConnection conn, AppDbContext db)
    {
        if (db.Routes.Any()) return 0;
        int count = 0;
        using var cmd = new OleDbCommand("SELECT * FROM [route_of_administration]", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            db.Routes.Add(new RouteOfAdministration
            {
                RouteName    = reader["route_name"] as string,
                Abbreviation = reader["abbreviation"] as string,
                Category     = reader["category"] as string,
                Description  = reader["description"] as string,
            });
            count++;
        }
        db.SaveChanges();
        return count;
    }

    private static int ImportDosages(OleDbConnection conn, AppDbContext db)
    {
        if (db.Dosages.Any()) return 0;
        int count = 0;
        using var cmd = new OleDbCommand("SELECT * FROM [Dosage]", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            db.Dosages.Add(new Dosage
            {
                DosageText = reader["Dosage"] as string,
                Type       = reader["Type"] as string,
                Category   = reader["category"] as string,
            });
            count++;
        }
        db.SaveChanges();
        return count;
    }

    private static int ImportMedicineNotes(OleDbConnection conn, AppDbContext db)
    {
        if (db.MedicineNotes.Any()) return 0;
        int count = 0;
        using var cmd = new OleDbCommand("SELECT * FROM [Medicine Notes]", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            db.MedicineNotes.Add(new MedicineNote { Notes = reader["Notes"] as string });
            count++;
        }
        db.SaveChanges();
        return count;
    }

    private static int ImportPrescriptionNotes(OleDbConnection conn, AppDbContext db)
    {
        if (db.PrescriptionNotes.Any()) return 0;
        int count = 0;
        using var cmd = new OleDbCommand("SELECT * FROM [Prescription Notes]", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            db.PrescriptionNotes.Add(new PrescriptionNote { Notes = reader["Notes"] as string });
            count++;
        }
        db.SaveChanges();
        return count;
    }

    private static int ImportLabTests(OleDbConnection conn, AppDbContext db)
    {
        if (db.LabTests.Any()) return 0;
        int count = 0;
        using var cmd = new OleDbCommand("SELECT * FROM [Lab Tests]", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            db.LabTests.Add(new LabTest
            {
                Category     = reader["Category"] as string,
                TestName     = reader["Test Name"] as string,
                Abbreviation = reader["Abbreviation"] as string,
                Specimen     = reader["Specimen"] as string,
                Description  = reader["Description"] as string,
            });
            count++;
        }
        db.SaveChanges();
        return count;
    }

    private static int ImportMedicineList(OleDbConnection conn, AppDbContext db)
    {
        if (db.MedicineLists.Any()) return 0;
        int count = 0;
        using var cmd = new OleDbCommand("SELECT * FROM [Medicine List]", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            db.MedicineLists.Add(new MedicineList
            {
                MedicineName = reader["Medicine Name"] as string,
                GenericName  = reader["Generic Name"] as string,
                Category     = reader["Category"] as string,
                Type         = reader["Type"] as string,
                Strength     = reader["Strength"] as string,
                Note         = reader["Note"] as string,
            });
            count++;
        }
        db.SaveChanges();
        return count;
    }

    private static int ImportPatients(OleDbConnection conn, AppDbContext db)
    {
        if (db.Patients.Any()) return 0;

        // Build physician name → ID lookup
        var physicianMap = db.Physicians
            .Where(p => p.NameDari != null)
            .ToDictionary(p => p.NameDari!, p => p.Id);

        // Read all rows from Access into memory first
        var rawRows = new List<(
            string?   PhysicianName,
            DateTime? OpdDate,
            string?   HijriDate,
            string?   PatientName,
            int?      Age,
            string?   Sex,
            string?   PhoneNumber,
            string?   BP, string? HR, string? PR, string? RR, string? BT, string? BW,
            string?   ClinicalFindings,
            string?   Diagnosis,
            string?   Note,
            DateTime? LastUpdated
        )>();

        using (var cmd = new OleDbCommand("SELECT * FROM [Patients Table]", conn))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                rawRows.Add((
                    reader["Physician_name"]    as string,
                    reader["OPD Date"]          as DateTime?,
                    reader["Hijri Date"]        as string,
                    reader["Patient Name"]      as string,
                    reader["Age"]               as int?,
                    reader["Sex"]               as string,
                    reader["Patient_Number"]    as string,
                    reader["BP"]                as string,
                    reader["HR"]                as string,
                    reader["PR"]                as string,
                    reader["RR"]                as string,
                    reader["BT"]                as string,
                    reader["BW"]                as string,
                    reader["Clinical Findings"] as string,
                    reader["Diagnosis"]         as string,
                    reader["Note_1"]            as string,
                    reader["Last_updated"]      as DateTime?
                ));
            }
        }

        // Create Patient demographic records first (to get auto-increment IDs)
        var patients = rawRows.Select(r => new Patient
        {
            PatientName = r.PatientName,
            Sex         = r.Sex,
            PhoneNumber = r.PhoneNumber,
            CreatedAt   = DateTime.UtcNow,
        }).ToList();

        foreach (var p in patients)
            db.Patients.Add(p);
        db.SaveChanges();

        // Generate PatientCodes + create Visit records (one per imported row)
        for (int i = 0; i < patients.Count; i++)
        {
            var p = patients[i];
            var r = rawRows[i];

            p.PatientCode = $"P-{p.Id:D5}";

            physicianMap.TryGetValue(r.PhysicianName ?? "", out var physicianId);

            db.Visits.Add(new Visit
            {
                PatientId        = p.Id,
                PhysicianId      = physicianId > 0 ? physicianId : null,
                OpdDate          = r.OpdDate,
                HijriDate        = r.HijriDate,
                Age              = r.Age,
                BP               = r.BP,
                HR               = r.HR,
                PR               = r.PR,
                RR               = r.RR,
                BT               = r.BT,
                BW               = r.BW,
                ClinicalFindings = r.ClinicalFindings,
                Diagnosis        = r.Diagnosis,
                Note             = r.Note,
                LastUpdated      = r.LastUpdated,
            });
        }
        db.SaveChanges();

        return patients.Count;
    }

    private static int ImportMedicineUsage(OleDbConnection conn, AppDbContext db)
    {
        if (db.MedicineUsages.Any()) return 0;
        int count = 0;

        // Map Access patient ID → SQLite visit ID
        var accessIdToVisitId = BuildPatientToVisitIdMap(conn, db);

        using var cmd = new OleDbCommand("SELECT * FROM [Medicine Usage]", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var accessPatientId = reader["F_Key"] as int?;
            if (accessPatientId is null) continue;
            if (!accessIdToVisitId.TryGetValue(accessPatientId.Value, out var visitId)) continue;

            db.MedicineUsages.Add(new MedicineUsage
            {
                VisitId      = visitId,
                LineNumber   = reader["Custom_ID"] as int? ?? 0,
                Type         = reader["Type"] as string,
                Prescription = reader["Prescription"] as string,
                Strength     = reader["Strength"] as string,
                Qty          = reader["Qty"] as int?,
                Usage        = reader["Usage"] as string,
                RouteName    = reader["route_name"] as string,
                Note         = reader["Note"] as string,
            });
            count++;
        }
        db.SaveChanges();
        return count;
    }

    /// <summary>
    /// Returns a mapping of Access patient ID → SQLite Visit ID.
    /// Matches by phone number + OPD date (same criteria as before, now resolving to Visit).
    /// </summary>
    private static Dictionary<int, int> BuildPatientToVisitIdMap(OleDbConnection conn, AppDbContext db)
    {
        var accessRows = new List<(int AccessId, string? Number, DateTime? Date)>();
        using var cmd = new OleDbCommand(
            "SELECT ID, [Patient_Number], [OPD Date] FROM [Patients Table]", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            accessRows.Add((
                (int)reader["ID"],
                reader["Patient_Number"] as string,
                reader["OPD Date"] as DateTime?
            ));

        // Load visits joined to patient phone + visit date
        var sqliteVisits = db.Visits
            .Select(v => new
            {
                VisitId     = v.Id,
                PhoneNumber = v.Patient != null ? v.Patient.PhoneNumber : null,
                v.OpdDate
            })
            .ToList();

        var map = new Dictionary<int, int>();
        foreach (var (accessId, number, date) in accessRows)
        {
            var match = sqliteVisits.FirstOrDefault(v =>
                v.PhoneNumber == number && v.OpdDate == date);
            if (match is not null)
                map[accessId] = match.VisitId;
        }
        return map;
    }
}

public class MigrationResult
{
    public bool    Success          { get; set; }
    public string? Error            { get; set; }
    public int     Physicians       { get; set; }
    public int     MedicineForms    { get; set; }
    public int     Routes           { get; set; }
    public int     Dosages          { get; set; }
    public int     MedicineNotes    { get; set; }
    public int     PrescriptionNotes { get; set; }
    public int     LabTests         { get; set; }
    public int     Medicines        { get; set; }
    public int     Patients         { get; set; }
    public int     Prescriptions    { get; set; }
}
