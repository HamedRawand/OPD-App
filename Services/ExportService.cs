using System.IO;
using ClosedXML.Excel;
using Microsoft.Win32;
using OPDClinic.Models;

namespace OPDClinic.Services;

/// <summary>
/// Exports data collections to Excel (.xlsx) or CSV files.
/// Call the static Export* methods; each shows a SaveFileDialog and writes the file.
/// Returns true if the file was saved, false if the user cancelled.
/// </summary>
public static class ExportService
{
    // ── Patients ──────────────────────────────────────────────────────────────

    public static bool ExportPatients(IEnumerable<Patient> patients)
    {
        var path = PickFile("Patients");
        if (path is null) return false;

        if (IsExcel(path))
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Patients");
            var headers = new[] { "Patient Code", "Patient Name", "Sex", "Phone Number", "Source Clinic" };
            WriteHeaders(ws, headers);

            int row = 2;
            foreach (var p in patients)
            {
                ws.Cell(row, 1).Value = p.PatientCode ?? "";
                ws.Cell(row, 2).Value = p.PatientName ?? "";
                ws.Cell(row, 3).Value = p.Sex ?? "";
                ws.Cell(row, 4).Value = p.PhoneNumber ?? "";
                ws.Cell(row, 5).Value = p.SourceClinic ?? "";
                row++;
            }
            AutoFit(ws, headers.Length);
            wb.SaveAs(path);
        }
        else
        {
            var lines = new List<string> { "Patient Code,Patient Name,Sex,Phone Number,Source Clinic" };
            foreach (var p in patients)
                lines.Add($"{Csv(p.PatientCode)},{Csv(p.PatientName)},{Csv(p.Sex)},{Csv(p.PhoneNumber)},{Csv(p.SourceClinic)}");
            File.WriteAllLines(path, lines, System.Text.Encoding.UTF8);
        }
        return true;
    }

    // ── Visit History (for one patient) ───────────────────────────────────────

    public static bool ExportVisits(IEnumerable<Visit> visits, Patient patient)
    {
        var path = PickFile($"Visits_{patient.PatientCode ?? patient.PatientName ?? "Patient"}");
        if (path is null) return false;

        if (IsExcel(path))
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Visits");
            var headers = new[] { "Date", "Hijri Date", "Next Visit", "Physician", "Age", "BP", "HR", "PR", "RR", "BT", "BW", "Diagnosis", "Clinical Findings", "Rx Count", "Lab Count" };
            WriteHeaders(ws, headers);

            int row = 2;
            foreach (var v in visits)
            {
                ws.Cell(row,  1).Value = v.OpdDate?.ToString("yyyy-MM-dd") ?? "";
                ws.Cell(row,  2).Value = v.HijriDate ?? "";
                ws.Cell(row,  3).Value = v.NextVisitDate ?? "";
                ws.Cell(row,  4).Value = v.Physician?.NameEng ?? "";
                ws.Cell(row,  5).Value = v.Age?.ToString() ?? "";
                ws.Cell(row,  6).Value = v.BP ?? "";
                ws.Cell(row,  7).Value = v.HR ?? "";
                ws.Cell(row,  8).Value = v.PR ?? "";
                ws.Cell(row,  9).Value = v.RR ?? "";
                ws.Cell(row, 10).Value = v.BT ?? "";
                ws.Cell(row, 11).Value = v.BW ?? "";
                ws.Cell(row, 12).Value = v.Diagnosis ?? "";
                ws.Cell(row, 13).Value = v.ClinicalFindings ?? "";
                ws.Cell(row, 14).Value = v.Medicines?.Count.ToString() ?? "0";
                ws.Cell(row, 15).Value = v.LabTests?.Count.ToString() ?? "0";
                row++;
            }
            AutoFit(ws, headers.Length);
            wb.SaveAs(path);
        }
        else
        {
            var lines = new List<string> { "Date,Hijri Date,Next Visit,Physician,Age,BP,HR,PR,RR,BT,BW,Diagnosis,Clinical Findings,Rx Count,Lab Count" };
            foreach (var v in visits)
                lines.Add($"{Csv(v.OpdDate?.ToString("yyyy-MM-dd"))},{Csv(v.HijriDate)},{Csv(v.NextVisitDate)},{Csv(v.Physician?.NameEng)},{v.Age},{Csv(v.BP)},{Csv(v.HR)},{Csv(v.PR)},{Csv(v.RR)},{Csv(v.BT)},{Csv(v.BW)},{Csv(v.Diagnosis)},{Csv(v.ClinicalFindings)},{v.Medicines?.Count ?? 0},{v.LabTests?.Count ?? 0}");
            File.WriteAllLines(path, lines, System.Text.Encoding.UTF8);
        }
        return true;
    }

    // ── Physicians ────────────────────────────────────────────────────────────

    public static bool ExportPhysicians(IEnumerable<Physician> physicians)
    {
        var path = PickFile("Physicians");
        if (path is null) return false;

        if (IsExcel(path))
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Physicians");
            var headers = new[] { "Name (English)", "Name (Dari)", "Clinic Name (EN)", "Clinic Name (Dari)", "Tagline", "Speciality (EN)", "Speciality (Dari)", "Other Speciality (EN)", "Other Speciality (Dari)", "Contact", "WhatsApp", "Reception", "Address" };
            WriteHeaders(ws, headers);

            int row = 2;
            foreach (var p in physicians)
            {
                ws.Cell(row,  1).Value = p.NameEng ?? "";
                ws.Cell(row,  2).Value = p.NameDari ?? "";
                ws.Cell(row,  3).Value = p.ClinicNameEng ?? "";
                ws.Cell(row,  4).Value = p.ClinicNameDari ?? "";
                ws.Cell(row,  5).Value = p.Tagline ?? "";
                ws.Cell(row,  6).Value = p.SpecialityEng ?? "";
                ws.Cell(row,  7).Value = p.SpecialityDari ?? "";
                ws.Cell(row,  8).Value = p.OtherSpecialityEng ?? "";
                ws.Cell(row,  9).Value = p.OtherSpecialityDari ?? "";
                ws.Cell(row, 10).Value = p.ContactNumber ?? "";
                ws.Cell(row, 11).Value = p.WhatsAppNumber ?? "";
                ws.Cell(row, 12).Value = p.ReceptionContactNumber ?? "";
                ws.Cell(row, 13).Value = p.Address ?? "";
                row++;
            }
            AutoFit(ws, headers.Length);
            wb.SaveAs(path);
        }
        else
        {
            var lines = new List<string> { "Name (English),Name (Dari),Clinic Name (EN),Clinic Name (Dari),Tagline,Speciality (EN),Speciality (Dari),Other Speciality (EN),Other Speciality (Dari),Contact,WhatsApp,Reception,Address" };
            foreach (var p in physicians)
                lines.Add($"{Csv(p.NameEng)},{Csv(p.NameDari)},{Csv(p.ClinicNameEng)},{Csv(p.ClinicNameDari)},{Csv(p.Tagline)},{Csv(p.SpecialityEng)},{Csv(p.SpecialityDari)},{Csv(p.OtherSpecialityEng)},{Csv(p.OtherSpecialityDari)},{Csv(p.ContactNumber)},{Csv(p.WhatsAppNumber)},{Csv(p.ReceptionContactNumber)},{Csv(p.Address)}");
            File.WriteAllLines(path, lines, System.Text.Encoding.UTF8);
        }
        return true;
    }

    // ── Medicine Catalog ──────────────────────────────────────────────────────

    public static bool ExportMedicines(IEnumerable<MedicineList> medicines)
    {
        var path = PickFile("Medicines");
        if (path is null) return false;

        if (IsExcel(path))
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Medicines");
            var headers = new[] { "Medicine Name", "Generic Name", "Category", "Type / Form", "Strength(s)", "Note" };
            WriteHeaders(ws, headers);

            int row = 2;
            foreach (var m in medicines)
            {
                ws.Cell(row, 1).Value = m.MedicineName ?? "";
                ws.Cell(row, 2).Value = m.GenericName ?? "";
                ws.Cell(row, 3).Value = m.Category ?? "";
                ws.Cell(row, 4).Value = m.Type ?? "";
                ws.Cell(row, 5).Value = m.StrengthsDisplay;
                ws.Cell(row, 6).Value = m.Note ?? "";
                row++;
            }
            AutoFit(ws, headers.Length);
            wb.SaveAs(path);
        }
        else
        {
            var lines = new List<string> { "Medicine Name,Generic Name,Category,Type / Form,Strength(s),Note" };
            foreach (var m in medicines)
                lines.Add($"{Csv(m.MedicineName)},{Csv(m.GenericName)},{Csv(m.Category)},{Csv(m.Type)},{Csv(m.StrengthsDisplay)},{Csv(m.Note)}");
            File.WriteAllLines(path, lines, System.Text.Encoding.UTF8);
        }
        return true;
    }

    // ── Options tables ────────────────────────────────────────────────────────

    public static bool ExportRoutes(IEnumerable<RouteOfAdministration> items)
    {
        var path = PickFile("Routes");
        if (path is null) return false;
        WriteSimpleExport(path, "Routes",
            new[] { "Category", "Route Name", "Abbreviation", "Description" },
            "Category,Route Name,Abbreviation,Description",
            items.Select(r => new[] { r.Category ?? "", r.RouteName ?? "", r.Abbreviation ?? "", r.Description ?? "" }));
        return true;
    }

    public static bool ExportDosages(IEnumerable<Dosage> items)
    {
        var path = PickFile("Dosages");
        if (path is null) return false;
        WriteSimpleExport(path, "Dosages",
            new[] { "Category", "Type", "Dosage Text" },
            "Category,Type,Dosage Text",
            items.Select(d => new[] { d.Category ?? "", d.Type ?? "", d.DosageText ?? "" }));
        return true;
    }

    public static bool ExportMedicineCategories(IEnumerable<MedicineForm> items)
    {
        var path = PickFile("MedicineCategories");
        if (path is null) return false;
        WriteSimpleExport(path, "MedicineCategories",
            new[] { "Category", "Form Name", "Abbreviation", "Note" },
            "Category,Form Name,Abbreviation,Note",
            items.Select(f => new[] { f.Category ?? "", f.FormName ?? "", f.Abbreviation ?? "", f.Note ?? "" }));
        return true;
    }

    public static bool ExportMedicineNotes(IEnumerable<MedicineNote> items)
    {
        var path = PickFile("MedicineNotes");
        if (path is null) return false;
        WriteSimpleExport(path, "MedicineNotes",
            new[] { "Category", "Type", "Notes" },
            "Category,Type,Notes",
            items.Select(n => new[] { n.Category ?? "", n.Type ?? "", n.Notes ?? "" }));
        return true;
    }

    public static bool ExportPrescriptionNotes(IEnumerable<PrescriptionNote> items)
    {
        var path = PickFile("PrescriptionNotes");
        if (path is null) return false;
        WriteSimpleExport(path, "PrescriptionNotes",
            new[] { "Notes" }, "Notes",
            items.Select(n => new[] { n.Notes ?? "" }));
        return true;
    }

    public static bool ExportLabTests(IEnumerable<LabTest> items)
    {
        var path = PickFile("LabTests");
        if (path is null) return false;
        WriteSimpleExport(path, "LabTests",
            new[] { "Category", "Test Name", "Abbreviation", "Specimen", "Description" },
            "Category,Test Name,Abbreviation,Specimen,Description",
            items.Select(lt => new[] { lt.Category ?? "", lt.TestName ?? "", lt.Abbreviation ?? "", lt.Specimen ?? "", lt.Description ?? "" }));
        return true;
    }

    // ── Internal helpers ──────────────────────────────────────────────────────

    private static void WriteSimpleExport(string path, string sheetName, string[] headers, string csvHeader, IEnumerable<string[]> rows)
    {
        var rowList = rows.ToList();
        if (IsExcel(path))
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add(sheetName);
            WriteHeaders(ws, headers);
            int row = 2;
            foreach (var r in rowList)
            {
                for (int col = 0; col < r.Length; col++)
                    ws.Cell(row, col + 1).Value = r[col];
                row++;
            }
            AutoFit(ws, headers.Length);
            wb.SaveAs(path);
        }
        else
        {
            var lines = new List<string> { csvHeader };
            lines.AddRange(rowList.Select(r => string.Join(",", r.Select(Csv))));
            File.WriteAllLines(path, lines, System.Text.Encoding.UTF8);
        }
    }

    private static void WriteHeaders(IXLWorksheet ws, string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1565C0");
            cell.Style.Font.FontColor = XLColor.White;
        }
    }

    private static void AutoFit(IXLWorksheet ws, int colCount)
    {
        for (int i = 1; i <= colCount; i++)
            ws.Column(i).AdjustToContents();
    }

    private static string? PickFile(string defaultName)
    {
        var dlg = new SaveFileDialog
        {
            Title            = "Export Data",
            FileName         = defaultName,
            Filter           = "Excel Workbook (*.xlsx)|*.xlsx|CSV File (*.csv)|*.csv",
            DefaultExt       = "xlsx",
            FilterIndex      = 1
        };
        return dlg.ShowDialog() == true ? dlg.FileName : null;
    }

    private static bool IsExcel(string path) =>
        path.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase);

    /// <summary>RFC-4180 CSV cell escaping.</summary>
    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        value = value.Replace("\"", "\"\"");
        return value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value}\"" : value;
    }
}
