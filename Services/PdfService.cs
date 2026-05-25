using System.IO;
using Microsoft.EntityFrameworkCore;
using OPDClinic.Data;
using OPDClinic.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace OPDClinic.Services;

// ── Data record passed to the PDF document ──────────────────────────────────
public record PrescriptionData(
    Patient             Patient,
    Physician?          Physician,
    List<MedicineUsage> Lines,
    List<LabTest>       LabTests,
    string?             FooterNote
);

// ── Service: loads data from DB and returns a temp PDF path ─────────────────
public class PdfService(AppDbContext db)
{
    public string GenerateForPatient(int patientId)
    {
        var patient = db.Patients
            .Include(p => p.Physician)
            .FirstOrDefault(p => p.Id == patientId)
            ?? throw new InvalidOperationException("Patient not found.");

        var lines = db.MedicineUsages
            .Where(m => m.PatientId == patientId)
            .OrderBy(m => m.LineNumber)
            .ToList();

        var labTests = db.PatientLabTests
            .Include(pt => pt.LabTest)
            .Where(pt => pt.PatientId == patientId)
            .OrderBy(pt => pt.LabTest!.Category)
            .ThenBy(pt => pt.LabTest!.TestName)
            .Select(pt => pt.LabTest!)
            .ToList();

        var data = new PrescriptionData(
            patient, patient.Physician, lines, labTests,
            string.IsNullOrWhiteSpace(patient.FooterNote) ? null : patient.FooterNote);

        var safeName  = string.Concat((patient.PatientName ?? "Patient")
            .Where(c => char.IsLetterOrDigit(c) || c == '_'));
        var visitDate = patient.OpdDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd");
        var path      = Path.Combine(Path.GetTempPath(),
                            $"Rx_{safeName}_{visitDate}_{DateTime.Now:HHmmss}.pdf");

        new PrescriptionDocument(data).GeneratePdf(path);
        return path;
    }
}

// ── QuestPDF Document ────────────────────────────────────────────────────────
public class PrescriptionDocument(PrescriptionData data) : IDocument
{
    // ── Settings (loaded once per document render) ────────────────────────────
    private readonly ReportSettings _s = ReportSettingsService.Current;

    // ── Section style shortcuts ────────────────────────────────────────────────
    private SectionStyle S_Hdr  => _s.Header;
    private SectionStyle S_PBar => _s.PatientBar;
    private SectionStyle S_VS   => _s.VitalSigns;
    private SectionStyle S_CF   => _s.ClinicalFindings;
    private SectionStyle S_Dx   => _s.Diagnosis;
    private SectionStyle S_LT   => _s.LabTests;
    private SectionStyle S_Rx   => _s.RxSection;
    private SectionStyle S_Ftr  => _s.Footer;

    // ── Accent color: taken from Header font color (#1565C0 by default) ────────
    private string Accent => Clr(S_Hdr.FontColor);

    // ── Muted secondary text (not exposed in settings — always a mid-gray) ─────
    private string Gray => Clr("#555555");

    // ── B&W helper: converts hex color to luminance-weighted grayscale ─────────
    private string Clr(string hex) => _s.BlackAndWhiteMode ? ToGray(hex) : hex;

    private static string ToGray(string hex)
    {
        try
        {
            var h = hex.TrimStart('#');
            if (h.Length == 6)
            {
                int r    = Convert.ToInt32(h[0..2], 16);
                int g    = Convert.ToInt32(h[2..4], 16);
                int b    = Convert.ToInt32(h[4..6], 16);
                int gray = (int)(r * 0.299 + g * 0.587 + b * 0.114);
                return $"#{gray:X2}{gray:X2}{gray:X2}";
            }
        }
        catch { }
        return hex;
    }

    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.MarginHorizontal(1.8f, Unit.Centimetre);
            page.MarginTop(1.2f,        Unit.Centimetre);
            page.MarginBottom(1.0f,     Unit.Centimetre);
            page.DefaultTextStyle(s =>
                s.FontSize(10)
                 .FontFamily(_s.GlobalFontFamily)
                 .FontColor(Clr("#111111")));

            page.Header().Element(ComposeHeader);
            page.Content().PaddingTop(10).Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    // ── HEADER: [EN info]  [Logo]  [Dari info] ───────────────────────────────
    private void ComposeHeader(IContainer c)
    {
        var ph   = data.Physician;
        var hdr  = S_Hdr;
        float fs = hdr.FontSize; // default 17f

        c.Column(col =>
        {
            col.Item().Row(row =>
            {
                // ── Left: English clinic info ──────────────────────────────
                row.RelativeItem().Column(info =>
                {
                    info.Item()
                        .Text(ph?.NameEng ?? "OPD Clinic")
                        .FontFamily(hdr.FontFamily)
                        .FontSize(fs).Bold().FontColor(Accent);

                    if (!string.IsNullOrWhiteSpace(ph?.SpecialityEng))
                        info.Item().PaddingTop(2)
                            .Text(ph.SpecialityEng)
                            .FontFamily(hdr.FontFamily)
                            .FontSize(Math.Max(fs - 7, 8)).FontColor(Clr("#111111"));

                    if (!string.IsNullOrWhiteSpace(ph?.OtherSpecialityEng))
                        info.Item().PaddingTop(1)
                            .Text(ph.OtherSpecialityEng)
                            .FontFamily(hdr.FontFamily)
                            .FontSize(Math.Max(fs - 8, 7)).FontColor(Gray);
                });

                // ── Centre: Logo / initials circle ────────────────────────
                row.ConstantItem(94).AlignCenter().AlignMiddle().Element(av =>
                {
                    if (ph?.SymbolImage is { Length: > 0 } img)
                    {
                        av.Width(76).Height(76).Image(img).FitArea();
                    }
                    else
                    {
                        av.Width(66).Height(66)
                          .Border(2).BorderColor(Clr(S_VS.BorderColor))
                          .Background(Clr(S_VS.TitleBgColor))
                          .AlignCenter().AlignMiddle()
                          .Text(GetInitials(ph?.NameEng ?? "OPD"))
                          .FontSize(22).Bold().FontColor(Accent);
                    }
                });

                // ── Right: Dari clinic info (RTL) ─────────────────────────
                row.RelativeItem().Column(info =>
                {
                    info.Item().Element(e =>
                        e.AlignRight().ContentFromRightToLeft()
                         .Text(ph?.NameDari ?? "کلینیک سرپایی")
                         .FontFamily(hdr.FontFamily)
                         .FontSize(Math.Max(fs - 1, 8)).Bold().FontColor(Accent));

                    if (!string.IsNullOrWhiteSpace(ph?.SpecialityDari))
                        info.Item().PaddingTop(2).Element(e =>
                            e.AlignRight().ContentFromRightToLeft()
                             .Text(ph.SpecialityDari)
                             .FontFamily(hdr.FontFamily)
                             .FontSize(Math.Max(fs - 7, 8)).FontColor(Clr("#111111")));

                    if (!string.IsNullOrWhiteSpace(ph?.OtherSpecialityDari))
                        info.Item().PaddingTop(1).Element(e =>
                            e.AlignRight().ContentFromRightToLeft()
                             .Text(ph.OtherSpecialityDari)
                             .FontFamily(hdr.FontFamily)
                             .FontSize(Math.Max(fs - 8, 7)).FontColor(Gray));
                });
            });

            // Divider line
            col.Item().PaddingTop(8).BorderBottom(2.5f).BorderColor(Accent);
        });
    }

    // ── CONTENT ──────────────────────────────────────────────────────────────
    private void ComposeContent(IContainer c)
    {
        c.Column(col =>
        {
            col.Item().Element(ComposePatientBar);
            col.Item().PaddingTop(10).Element(ComposeBody);

            if (!string.IsNullOrWhiteSpace(data.FooterNote))
                col.Item().PaddingTop(10).Element(ComposeFooterNote);
        });
    }

    // ── PATIENT INFO BAR ─────────────────────────────────────────────────────
    private void ComposePatientBar(IContainer c)
    {
        var p   = data.Patient;
        var pb  = S_PBar;
        float fs = pb.FontSize; // default 9f

        var container = c.Background(Clr(pb.BackgroundColor));
        if (pb.ShowBorder)
            container = container.BorderLeft(pb.BorderThickness).BorderColor(Clr(pb.BorderColor));

        container.Padding(pb.Padding).Row(row =>
        {
            // Shamsi visit date — leftmost
            row.RelativeItem(2).AlignLeft().Element(e =>
                e.ContentFromRightToLeft().Text(text =>
                {
                    text.Span("تاریخ مراجعه:  ").Bold().FontFamily(pb.FontFamily).FontSize(fs);
                    text.Span(p.HijriDate ?? p.OpdDate?.ToString("yyyy-MM-dd") ?? "—")
                        .FontFamily(pb.FontFamily).FontSize(fs);
                }));

            // Age + Gender — two stacked rows in the centre
            row.RelativeItem(2).AlignCenter().Column(col =>
            {
                col.Item().AlignCenter().Element(e =>
                    e.ContentFromRightToLeft().Text(text =>
                    {
                        text.Span("سن مریض:  ").Bold().FontFamily(pb.FontFamily).FontSize(fs);
                        text.Span(p.Age.HasValue ? p.Age.Value.ToString() : "—")
                            .FontFamily(pb.FontFamily).FontSize(fs);
                    }));
                col.Item().AlignCenter().PaddingTop(4).Element(e =>
                    e.ContentFromRightToLeft().Text(text =>
                    {
                        text.Span("جنسیت مریض:  ").Bold().FontFamily(pb.FontFamily).FontSize(fs);
                        text.Span(TranslateSex(CleanFieldValue(p.Sex)))
                            .FontFamily(pb.FontFamily).FontSize(fs);
                    }));
            });

            // Name — rightmost, RTL (no patient number)
            row.RelativeItem(3).AlignRight().Element(e =>
                e.ContentFromRightToLeft().Text(text =>
                {
                    text.Span("نام مریض:  ").Bold().FontFamily(pb.FontFamily).FontSize(fs);
                    text.Span(p.PatientName ?? "—").Bold()
                        .FontFamily(pb.FontFamily).FontSize(fs + 2).FontColor(Accent);
                }));
        });
    }

    // ── BODY: [Clinical left] | [Rx right] ───────────────────────────────────
    private void ComposeBody(IContainer c)
    {
        c.Row(row =>
        {
            // Left: Vital Signs + Clinical Findings + Diagnosis + Lab Tests
            row.RelativeItem(1.4f).Column(col =>
            {
                col.Item().Element(ComposeVitalsBox);
                col.Item().PaddingTop(6).Element(ctx =>
                    ComposeLabelledBox(ctx, "Clinical Findings", data.Patient.ClinicalFindings, S_CF));
                col.Item().PaddingTop(6).Element(ctx =>
                    ComposeLabelledBox(ctx, "Diagnosis", data.Patient.Diagnosis, S_Dx));

                if (data.LabTests.Any())
                    col.Item().PaddingTop(6).Element(ComposeLabTests);
            });

            // Separator gap
            row.ConstantItem(14);

            // Right: Rx only
            row.RelativeItem(2.6f).Column(col =>
            {
                col.Item().Element(ComposeRxSection);
            });
        });
    }

    // ── VITAL SIGNS BOX ──────────────────────────────────────────────────────
    private void ComposeVitalsBox(IContainer c)
    {
        var p   = data.Patient;
        var st  = S_VS;
        float fs = st.FontSize; // default 9f

        var outer = c;
        if (st.ShowBorder)
            outer = outer.Border(st.BorderThickness).BorderColor(Clr(st.BorderColor));

        outer.Column(col =>
        {
            // Title bar
            var titleRow = col.Item().Background(Clr(st.TitleBgColor)).Padding(5);
            var titleText = titleRow.Text("Vital Signs")
                .FontFamily(st.FontFamily)
                .FontSize(Math.Max(fs - 0.5f, 6))
                .FontColor(Clr(st.TitleFontColor));
            if (st.TitleBold) titleText.Bold();

            col.Item().Padding(st.Padding).Column(v =>
            {
                // BP + PR
                if (!string.IsNullOrEmpty(p.BP) || !string.IsNullOrEmpty(p.PR))
                    v.Item().PaddingBottom(2).Row(r =>
                    {
                        r.AutoItem().Text("BP: ").FontFamily(st.FontFamily).FontSize(fs).Bold();
                        r.AutoItem().Text(p.BP ?? "—").FontFamily(st.FontFamily).FontSize(fs);
                        r.ConstantItem(18); // spacer between pairs
                        r.AutoItem().Text("PR: ").FontFamily(st.FontFamily).FontSize(fs).Bold();
                        r.AutoItem().Text(p.PR ?? "—").FontFamily(st.FontFamily).FontSize(fs);
                    });

                // RR
                if (!string.IsNullOrEmpty(p.RR))
                    v.Item().PaddingTop(6).Row(r =>
                    {
                        r.AutoItem().Text("RR: ").FontFamily(st.FontFamily).FontSize(fs).Bold();
                        r.AutoItem().Text(p.RR).FontFamily(st.FontFamily).FontSize(fs);
                    });

                // BT + BW
                if (!string.IsNullOrEmpty(p.BT) || !string.IsNullOrEmpty(p.BW))
                    v.Item().PaddingTop(6).Row(r =>
                    {
                        r.AutoItem().Text("BT: ").FontFamily(st.FontFamily).FontSize(fs).Bold();
                        r.AutoItem().Text(p.BT ?? "—").FontFamily(st.FontFamily).FontSize(fs);
                        r.ConstantItem(18); // spacer between pairs
                        r.AutoItem().Text("BW: ").FontFamily(st.FontFamily).FontSize(fs).Bold();
                        r.AutoItem().Text(p.BW ?? "—").FontFamily(st.FontFamily).FontSize(fs);
                    });

                // All empty
                var anyVital = !string.IsNullOrEmpty(p.BP) || !string.IsNullOrEmpty(p.PR)
                            || !string.IsNullOrEmpty(p.RR) || !string.IsNullOrEmpty(p.BT)
                            || !string.IsNullOrEmpty(p.BW);
                if (!anyVital)
                    v.Item().Text("—").FontFamily(st.FontFamily)
                            .FontSize(Math.Max(fs - 0.5f, 6)).FontColor(Gray).Italic();
            });
        });
    }

    // ── LABELLED BOX (Clinical Findings / Diagnosis / Lab Tests) ─────────────
    private void ComposeLabelledBox(IContainer c, string title, string? content, SectionStyle st)
    {
        float fs = st.FontSize; // default 9f

        var outer = c;
        if (st.ShowBorder)
            outer = outer.Border(st.BorderThickness).BorderColor(Clr(st.BorderColor));

        outer.Column(col =>
        {
            // Title bar
            var titleRow  = col.Item().Background(Clr(st.TitleBgColor)).Padding(5);
            var titleText = titleRow.Text(title)
                .FontFamily(st.FontFamily)
                .FontSize(Math.Max(fs - 0.5f, 6))
                .FontColor(Clr(st.TitleFontColor));
            if (st.TitleBold) titleText.Bold();

            col.Item().Padding(st.Padding)
               .Text(string.IsNullOrWhiteSpace(content) ? "—" : content)
               .FontFamily(st.FontFamily).FontSize(fs).FontColor(Clr(st.FontColor));
        });
    }

    // ── LAB TESTS ────────────────────────────────────────────────────────────
    private void ComposeLabTests(IContainer c)
    {
        var st  = S_LT;
        float fs = st.FontSize;

        var outer = c;
        if (st.ShowBorder)
            outer = outer.Border(st.BorderThickness).BorderColor(Clr(st.BorderColor));

        // Group tests by category — null/empty category falls back to "General"
        var groups = data.LabTests
            .GroupBy(t => string.IsNullOrWhiteSpace(t.Category) ? "General" : t.Category)
            .OrderBy(g => g.Key)
            .ToList();

        bool multiGroup = groups.Count > 1;

        outer.Column(col =>
        {
            // Title bar
            var titleRow  = col.Item().Background(Clr(st.TitleBgColor)).Padding(5);
            var titleText = titleRow.Text("Lab Tests")
                .FontFamily(st.FontFamily)
                .FontSize(Math.Max(fs - 0.5f, 6))
                .FontColor(Clr(st.TitleFontColor));
            if (st.TitleBold) titleText.Bold();

            // Content — one bullet line per test, grouped by category
            col.Item().Padding(st.Padding).Column(body =>
            {
                foreach (var grp in groups)
                {
                    if (multiGroup)
                    {
                        // Category heading (bold, slightly smaller)
                        var catText = body.Item().PaddingBottom(3)
                            .Text(grp.Key)
                            .FontFamily(st.FontFamily)
                            .FontSize(Math.Max(fs - 0.5f, 6))
                            .FontColor(Clr(st.FontColor));
                        catText.Bold();
                    }

                    // Each test on its own line with a bullet
                    foreach (var t in grp)
                    {
                        var label = string.IsNullOrEmpty(t.Abbreviation)
                            ? t.TestName ?? ""
                            : $"{t.TestName} ({t.Abbreviation})";

                        body.Item()
                            .PaddingLeft(multiGroup ? 10 : 0)
                            .PaddingBottom(3)
                            .Text($"•  {label}")
                            .FontFamily(st.FontFamily).FontSize(fs).FontColor(Clr(st.FontColor));
                    }

                    // Gap between groups
                    if (multiGroup)
                        body.Item().Height(4);
                }
            });
        });
    }

    // ── Rx SECTION ───────────────────────────────────────────────────────────
    private void ComposeRxSection(IContainer c)
    {
        c.Column(col =>
        {
            // Large "Rx" heading — decorative, always uses accent color
            col.Item()
               .Text("Rx")
               .FontSize(24).Bold().Italic().FontColor(Accent);

            col.Item().PaddingTop(6).Element(ComposeRxLines);
        });
    }

    private void ComposeRxLines(IContainer c)
    {
        var st  = S_Rx;
        float fs = st.FontSize; // default 10.5f

        if (!data.Lines.Any())
        {
            c.Background(Clr(S_VS.TitleBgColor)).Padding(10).AlignCenter()
             .Text("No medicines prescribed.").FontColor(Gray).Italic().FontSize(fs - 1.5f);
            return;
        }

        c.Column(col =>
        {
            for (int i = 0; i < data.Lines.Count; i++)
            {
                var line = data.Lines[i];

                col.Item()
                   .BorderBottom(0.5f).BorderColor(Clr(S_Ftr.BorderColor))
                   .PaddingVertical(6).PaddingHorizontal(4)
                   .Column(entry =>
                   {
                       // ── Main row: [N)] [Form] [Medicine  Strength] [Dari dosage] ──
                       entry.Item().Row(row =>
                       {
                           // Number
                           row.ConstantItem(24)
                              .Text($"{i + 1} )")
                              .FontFamily(st.FontFamily).FontSize(fs).Bold().FontColor(Accent);

                           // Form (muted)
                           row.ConstantItem(58)
                              .Text(line.Type ?? "")
                              .FontFamily(st.FontFamily).FontSize(fs).FontColor(Gray);

                           // Medicine name + Strength
                           row.RelativeItem().Text(text =>
                           {
                               var nameSpan = text.Span(line.Prescription ?? "")
                                   .FontFamily(st.FontFamily).FontSize(fs);
                               if (st.Bold) nameSpan.Bold();
                               if (!string.IsNullOrEmpty(line.Strength))
                                   text.Span($"   {line.Strength}")
                                       .FontFamily(st.FontFamily)
                                       .FontSize(Math.Max(fs - 1.5f, 6)).FontColor(Gray);
                           });

                           // Dari dosage (RTL, right-aligned)
                           if (!string.IsNullOrEmpty(line.Usage))
                               row.RelativeItem().Element(e =>
                                   e.AlignRight().ContentFromRightToLeft()
                                    .Text(line.Usage)
                                    .FontFamily(st.FontFamily)
                                    .FontSize(Math.Max(fs - 1.5f, 6))
                                    .FontColor(Clr(st.FontColor)));
                       });

                       // ── N = qty ──────────────────────────────────────────
                       if (line.Qty.HasValue)
                           entry.Item().PaddingTop(2).PaddingLeft(82)
                                .Text($"N = {line.Qty}")
                                .FontFamily(st.FontFamily)
                                .FontSize(Math.Max(fs - 1.5f, 6)).FontColor(Gray);

                       // ── Medicine note (RTL, bold) ─────────────────────────
                       if (!string.IsNullOrEmpty(line.Note))
                           entry.Item().PaddingTop(3).Element(e =>
                               e.ContentFromRightToLeft()
                                .Text($"نوت :  {line.Note}")
                                .FontFamily(st.FontFamily)
                                .FontSize(Math.Max(fs - 1.5f, 6))
                                .FontColor(Clr(st.FontColor)).Bold());
                   });
            }
        });
    }

    // ── FOOTER NOTE (Prescription Note — RTL) ────────────────────────────────
    private void ComposeFooterNote(IContainer c)
    {
        var st = S_Rx;
        c.BorderTop(1).BorderColor(Clr(S_Ftr.BorderColor))
         .PaddingTop(6)
         .ContentFromRightToLeft()
         .Text($"نوت :  {data.FooterNote}")
         .FontFamily(st.FontFamily)
         .FontSize(st.FontSize).FontColor(Clr(st.FontColor));
    }

    // ── PAGE FOOTER: contact info + address (RTL, right-aligned) ─────────────
    private void ComposeFooter(IContainer c)
    {
        var ph  = data.Physician;
        var st  = S_Ftr;
        float fs = st.FontSize; // default 8f

        string imgDir     = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "image");
        byte[]? phoneIcon = TryLoadImage(Path.Combine(imgDir, "PhoneNumber.png"));
        byte[]? waIcon    = TryLoadImage(Path.Combine(imgDir, "WhatsApp.png"));

        var outer = c;
        if (st.ShowBorder)
            outer = outer.BorderTop(st.BorderThickness).BorderColor(Clr(st.BorderColor));

        outer.PaddingTop(st.Padding).Column(col =>
        {
            bool hasContact   = !string.IsNullOrWhiteSpace(ph?.ContactNumber);
            bool hasWhatsApp  = !string.IsNullOrWhiteSpace(ph?.WhatsAppNumber);
            bool hasReception = !string.IsNullOrWhiteSpace(ph?.ReceptionContactNumber);

            if (hasContact || hasWhatsApp || hasReception)
            {
                col.Item().AlignRight().Row(row =>
                {
                    if (hasReception)
                    {
                        row.AutoItem()
                           .Text(ph!.ReceptionContactNumber)
                           .FontFamily(st.FontFamily).FontSize(fs).FontColor(Clr(st.FontColor));
                        row.ConstantItem(4);
                        row.AutoItem()
                           .Text(":نمبر معلومات").Bold()
                           .FontFamily(st.FontFamily).FontSize(fs).FontColor(Clr(st.FontColor));
                        row.ConstantItem(20);
                    }

                    if (hasWhatsApp)
                    {
                        row.AutoItem()
                           .Text(ph!.WhatsAppNumber)
                           .FontFamily(st.FontFamily).FontSize(fs).FontColor(Clr(st.FontColor));
                        row.ConstantItem(4);
                        if (waIcon != null)
                            row.AutoItem().Width(11).Height(11).Image(waIcon).FitArea();
                        row.ConstantItem(12);
                    }

                    if (hasContact)
                    {
                        row.AutoItem()
                           .Text(ph!.ContactNumber)
                           .FontFamily(st.FontFamily).FontSize(fs).FontColor(Clr(st.FontColor));
                        row.ConstantItem(4);
                        if (phoneIcon != null)
                            row.AutoItem().Width(11).Height(11).Image(phoneIcon).FitArea();
                    }

                    row.ConstantItem(10);
                    row.AutoItem()
                       .Text(":شماره دوکتور").Bold()
                       .FontFamily(st.FontFamily).FontSize(fs).FontColor(Clr(st.FontColor));
                });
            }

            // "Bring prescription on next visit" reminder — same RTL row style as other footer lines
            col.Item().PaddingTop(4).AlignRight().Row(row =>
            {
                row.AutoItem()
                   .Text(".در صورت مراجعه بعدی نسخه را با خود داشته باشید")
                   .FontFamily(st.FontFamily).FontSize(fs).FontColor(Clr(st.FontColor));
                row.ConstantItem(6);
                row.AutoItem()
                   .Text(":نوت").Bold()
                   .FontFamily(st.FontFamily).FontSize(fs).FontColor(Clr(st.FontColor));
            });

            if (!string.IsNullOrWhiteSpace(ph?.Address))
            {
                col.Item().PaddingTop(4).AlignRight().Row(row =>
                {
                    row.AutoItem()
                       .Text(ph.Address)
                       .FontFamily(st.FontFamily).FontSize(Math.Max(fs - 0.5f, 6)).FontColor(Gray);
                    row.ConstantItem(6);
                    row.AutoItem()
                       .Text(":آدرس").Bold()
                       .FontFamily(st.FontFamily).FontSize(fs).FontColor(Clr(st.FontColor));
                });
            }
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Safely reads an image file; returns null if the file is missing or unreadable.</summary>
    private static byte[]? TryLoadImage(string path)
    {
        try { return File.Exists(path) ? File.ReadAllBytes(path) : null; }
        catch { return null; }
    }

    /// <summary>Returns the canonical Dari sex label.
    /// Handles new Dari-stored values, legacy English values, and ComboBoxItem prefix.</summary>
    private static string TranslateSex(string value)
    {
        const string prefix = "System.Windows.Controls.ComboBoxItem: ";
        var clean = value.StartsWith(prefix, StringComparison.Ordinal)
            ? value[prefix.Length..] : value;
        return clean switch
        {
            "Male"   or "مذکر" => "مذکر",
            "Female" or "مؤنث" => "مؤنث",
            _                   => clean
        };
    }

    /// <summary>Strips the "System.Windows.Controls.ComboBoxItem: " prefix that
    /// WPF can inject when a ComboBox item is stored as its ToString() value.</summary>
    private static string CleanFieldValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "—";
        const string prefix = "System.Windows.Controls.ComboBoxItem: ";
        return value.StartsWith(prefix, StringComparison.Ordinal)
            ? value[prefix.Length..]
            : value;
    }

    private static string GetInitials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[^1][0]}".ToUpper()
            : name.Length >= 2 ? name[..2].ToUpper() : name.ToUpper();
    }
}
