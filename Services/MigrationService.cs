using System.Data.OleDb;
using Microsoft.EntityFrameworkCore;
using OPDClinic.Data;
using OPDClinic.Models;

namespace OPDClinic.Services;

/// <summary>
/// One-time importer: reads the original Access .accdb and populates SQLite.
/// Run from admin settings menu. Safe to call repeatedly — skips tables already populated.
/// </summary>
public class MigrationService(AppDbContext db)
{
    public MigrationResult Import(string accdbPath)
    {
        var result = new MigrationResult();

        try
        {
            var connStr = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={accdbPath};Persist Security Info=False;";
            using var conn = new OleDbConnection(connStr);
            conn.Open();

            result.Physicians   = ImportPhysicians(conn);
            result.MedicineForms = ImportMedicineForms(conn);
            result.Routes       = ImportRoutes(conn);
            result.Dosages      = ImportDosages(conn);
            result.MedicineNotes = ImportMedicineNotes(conn);
            result.PrescriptionNotes = ImportPrescriptionNotes(conn);
            result.LabTests     = ImportLabTests(conn);
            result.Medicines    = ImportMedicineList(conn);
            result.Patients     = ImportPatients(conn);
            result.Prescriptions = ImportMedicineUsage(conn);

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Error = ex.Message;
        }

        return result;
    }

    private int ImportPhysicians(OleDbConnection conn)
    {
        if (db.Physicians.Any()) return 0;
        int count = 0;
        using var cmd = new OleDbCommand("SELECT * FROM [Add Physician]", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            db.Physicians.Add(new Physician
            {
                NameEng            = reader["Physician_name_Eng"] as string,
                NameDari           = reader["Physician_name_Dari"] as string,
                SpecialityEng      = reader["Specialities_Eng"] as string,
                SpecialityDari     = reader["Specialities_Dari"] as string,
                OtherSpecialityEng = reader["Other_specialities_Eng"] as string,
                OtherSpecialityDari= reader["Other_specialities_Dari"] as string,
                ContactNumber      = reader["Contact_number"] as string,
                WhatsAppNumber     = reader["WhatsApp_number"] as string,
                ReceptionContactNumber = reader["Receiption_contact_number"] as string,
                Address            = reader["Address"] as string,
            });
            count++;
        }
        db.SaveChanges();
        return count;
    }

    private int ImportMedicineForms(OleDbConnection conn)
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

    private int ImportRoutes(OleDbConnection conn)
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

    private int ImportDosages(OleDbConnection conn)
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

    private int ImportMedicineNotes(OleDbConnection conn)
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

    private int ImportPrescriptionNotes(OleDbConnection conn)
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

    private int ImportLabTests(OleDbConnection conn)
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

    private int ImportMedicineList(OleDbConnection conn)
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

    private int ImportPatients(OleDbConnection conn)
    {
        if (db.Patients.Any()) return 0;
        int count = 0;

        // Build physician name → ID lookup
        var physicianMap = db.Physicians
            .Where(p => p.NameDari != null)
            .ToDictionary(p => p.NameDari!, p => p.Id);

        using var cmd = new OleDbCommand("SELECT * FROM [Patients Table]", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var physicianName = reader["Physician_name"] as string;
            int? physicianId = physicianName != null && physicianMap.TryGetValue(physicianName, out var pid)
                ? pid : null;

            db.Patients.Add(new Patient
            {
                PhysicianId      = physicianId,
                OpdDate          = reader["OPD Date"] as DateTime?,
                HijriDate        = reader["Hijri Date"] as string,
                PatientName      = reader["Patient Name"] as string,
                Age              = reader["Age"] as int?,
                Sex              = reader["Sex"] as string,
                PatientNumber    = reader["Patient_Number"] as string,
                BP               = reader["BP"] as string,
                HR               = reader["HR"] as string,
                PR               = reader["PR"] as string,
                RR               = reader["RR"] as string,
                BT               = reader["BT"] as string,
                BW               = reader["BW"] as string,
                ClinicalFindings = reader["Clinical Findings"] as string,
                Diagnosis        = reader["Diagnosis"] as string,
                Note             = reader["Note_1"] as string,
                LastUpdated      = reader["Last_updated"] as DateTime?,
            });
            count++;
        }
        db.SaveChanges();
        return count;
    }

    private int ImportMedicineUsage(OleDbConnection conn)
    {
        if (db.MedicineUsages.Any()) return 0;
        int count = 0;

        // Access F_Key maps to original Access Patient ID — we stored patients in insertion order.
        // Build a map: original Access ID → new SQLite Patient ID via OpdDate+PatientNumber.
        // Simpler: re-read Access patients to get original IDs.
        var accessIdToSqliteId = BuildPatientIdMap(conn);

        using var cmd = new OleDbCommand("SELECT * FROM [Medicine Usage]", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            var accessPatientId = reader["F_Key"] as int?;
            if (accessPatientId is null) continue;
            if (!accessIdToSqliteId.TryGetValue(accessPatientId.Value, out var sqlitePatientId)) continue;

            db.MedicineUsages.Add(new MedicineUsage
            {
                PatientId    = sqlitePatientId,
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

    private Dictionary<int, int> BuildPatientIdMap(OleDbConnection conn)
    {
        // Read Access patient IDs + a unique key (PatientNumber + OpdDate) to match SQLite rows
        var accessRows = new List<(int AccessId, string? Number, DateTime? Date)>();
        using var cmd = new OleDbCommand("SELECT ID, [Patient_Number], [OPD Date] FROM [Patients Table]", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            accessRows.Add((
                (int)reader["ID"],
                reader["Patient_Number"] as string,
                reader["OPD Date"] as DateTime?
            ));

        var sqlitePatients = db.Patients
            .Select(p => new { p.Id, p.PatientNumber, p.OpdDate })
            .ToList();

        var map = new Dictionary<int, int>();
        foreach (var (accessId, number, date) in accessRows)
        {
            var match = sqlitePatients.FirstOrDefault(p =>
                p.PatientNumber == number && p.OpdDate == date);
            if (match is not null)
                map[accessId] = match.Id;
        }
        return map;
    }
}

public class MigrationResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int Physicians { get; set; }
    public int MedicineForms { get; set; }
    public int Routes { get; set; }
    public int Dosages { get; set; }
    public int MedicineNotes { get; set; }
    public int PrescriptionNotes { get; set; }
    public int LabTests { get; set; }
    public int Medicines { get; set; }
    public int Patients { get; set; }
    public int Prescriptions { get; set; }
}
