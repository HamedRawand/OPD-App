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
    Visit               Visit,
    Physician?          Physician,
    List<MedicineUsage> Lines,
    List<LabTest>       LabTests,
    string?             FooterNote
);

// ── Service: loads data from DB and returns a temp PDF path ─────────────────
public class PdfService(IDbContextFactory<AppDbContext> factory)
{
    public string GenerateForVisit(int visitId)
    {
        using var db = factory.CreateDbContext();

        var visit = db.Visits
            .Include(v => v.Patient)
            .Include(v => v.Physician)
            .FirstOrDefault(v => v.Id == visitId)
            ?? throw new InvalidOperationException("Visit not found.");

        var patient = visit.Patient
            ?? throw new InvalidOperationException("Patient not found for this visit.");

        var lines = db.MedicineUsages
            .Where(m => m.VisitId == visitId)
            .OrderBy(m => m.LineNumber)
            .ToList();

        var labTests = db.PatientLabTests
            .Include(pt => pt.LabTest)
            .Where(pt => pt.VisitId == visitId)
            .OrderBy(pt => pt.LabTest!.Category)
            .ThenBy(pt => pt.LabTest!.TestName)
            .Select(pt => pt.LabTest!)
            .ToList();

        var data = new PrescriptionData(
            patient, visit, visit.Physician, lines, labTests,
            string.IsNullOrWhiteSpace(visit.FooterNote) ? null : visit.FooterNote);

        var safeName  = string.Concat((patient.PatientName ?? "Patient")
            .Where(c => char.IsLetterOrDigit(c) || c == '_'));
        var visitDate = visit.OpdDate?.ToString("yyyy-MM-dd") ?? DateTime.Now.ToString("yyyy-MM-dd");
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
    private SectionStyle S_DocEn   => _s.DoctorNameEn;
    private SectionStyle S_SpEn    => _s.SpecialityEn;
    private SectionStyle S_OtSpEn  => _s.OtherSpecialityEn;
    private SectionStyle S_DocDar  => _s.DoctorNameDari;
    private SectionStyle S_SpDar   => _s.SpecialityDari;
    private SectionStyle S_OtSpDar => _s.OtherSpecialityDari;
    private SectionStyle S_Tag     => _s.Tagline;
    private SectionStyle S_PBar => _s.PatientBar;
    private SectionStyle S_VS   => _s.VitalSigns;
    private SectionStyle S_CF   => _s.ClinicalFindings;
    private SectionStyle S_Dx   => _s.Diagnosis;
    private SectionStyle S_LT   => _s.LabTests;
    private SectionStyle S_Rx   => _s.RxSection;
    private SectionStyle S_Ftr  => _s.Footer;

    // ── Accent color: taken from EN header font color (#1565C0 by default) ─────
    private string Accent => Clr(S_DocEn.FontColor);

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
            page.Content().PaddingTop((float)_s.PatientBarGap).Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    // ── HEADER: [EN info ×2]  [Logo]  [Dari info ×1] ────────────────────────
    private void ComposeHeader(IContainer c)
    {
        var ph      = data.Physician;
        var docEn   = S_DocEn;
        var spEn    = S_SpEn;
        var otSpEn  = S_OtSpEn;
        var docDar  = S_DocDar;
        var spDar   = S_SpDar;
        var otSpDar = S_OtSpDar;

        c.Column(col =>
        {
            col.Item().Row(row =>
            {
                // ── Left (weight 1): English clinic info ───────────────────
                row.RelativeItem(1).Column(info =>
                {
                    if (!string.IsNullOrWhiteSpace(ph?.ClinicNameEng))
                    {
                        var cn = _s.ClinicNameEn;
                        info.Item().Text(text =>
                        {
                            if      (cn.TextAlign == "Center") text.AlignCenter();
                            else if (cn.TextAlign == "Right")  text.AlignRight();
                            var sp = text.Span(ph.ClinicNameEng)
                                .FontFamily(cn.FontFamily).FontSize(cn.FontSize)
                                .FontColor(Clr(cn.FontColor));
                            if (cn.Bold)   sp.Bold();
                            if (cn.Italic) sp.Italic();
                        });
                    }

                    info.Item().Text(text =>
                    {
                        if      (docEn.TextAlign == "Center") text.AlignCenter();
                        else if (docEn.TextAlign == "Right")  text.AlignRight();
                        var sp = text.Span(ph?.NameEng ?? "Rx Writer")
                            .FontFamily(docEn.FontFamily).FontSize(docEn.FontSize)
                            .FontColor(Clr(docEn.FontColor));
                        if (docEn.Bold)   sp.Bold();
                        if (docEn.Italic) sp.Italic();
                    });

                    if (!string.IsNullOrWhiteSpace(ph?.SpecialityEng))
                        info.Item().PaddingTop(2).Text(text =>
                        {
                            if      (spEn.TextAlign == "Center") text.AlignCenter();
                            else if (spEn.TextAlign == "Right")  text.AlignRight();
                            var sp = text.Span(ph.SpecialityEng)
                                .FontFamily(spEn.FontFamily).FontSize(spEn.FontSize)
                                .FontColor(Clr(spEn.FontColor));
                            if (spEn.Bold)   sp.Bold();
                            if (spEn.Italic) sp.Italic();
                        });

                    if (!string.IsNullOrWhiteSpace(ph?.OtherSpecialityEng))
                        info.Item().PaddingTop(1).Text(text =>
                        {
                            if      (otSpEn.TextAlign == "Center") text.AlignCenter();
                            else if (otSpEn.TextAlign == "Right")  text.AlignRight();
                            var sp = text.Span(ph.OtherSpecialityEng)
                                .FontFamily(otSpEn.FontFamily).FontSize(otSpEn.FontSize)
                                .FontColor(Clr(otSpEn.FontColor));
                            if (otSpEn.Bold)   sp.Bold();
                            if (otSpEn.Italic) sp.Italic();
                        });
                });

                // ── Centre: Logo / initials circle (+ tagline pinned to bottom) ─
                var logoSz    = (float)_s.LogoSize;
                var circleSz  = Math.Max(logoSz - 4f, 20f);
                var colWidth  = logoSz + 20f;
                row.ConstantItem(colWidth).AlignCenter().Column(logoCol =>
                {
                    logoCol.Item().AlignCenter().Element(av =>
                    {
                        if (ph?.SymbolImage is { Length: > 0 } img)
                        {
                            av.Width(logoSz).Height(logoSz).Image(img).FitArea();
                        }
                        else
                        {
                            av.Width(circleSz).Height(circleSz)
                              .Border(2).BorderColor(Clr(S_VS.BorderColor))
                              .Background(Clr(S_VS.TitleBgColor))
                              .AlignCenter().AlignMiddle()
                              .Text(GetInitials(ph?.NameEng ?? "OPD"))
                              .FontSize(Math.Max(circleSz * 0.34f, 10f)).Bold().FontColor(Accent);
                        }
                    });

                    if (!string.IsNullOrWhiteSpace(ph?.Tagline))
                    {
                        var tag = S_Tag;
                        var tagText = logoCol.Item().AlignCenter()
                               .Text(ph.Tagline)
                               .FontFamily(tag.FontFamily).FontSize(tag.FontSize)
                               .FontColor(Clr(tag.FontColor));
                        if (tag.Bold)   tagText.Bold();
                        if (tag.Italic) tagText.Italic();
                    }
                });

                // ── Right (weight 1): Dari clinic info (RTL) ──────────────
                row.RelativeItem(1).Column(info =>
                {
                    if (!string.IsNullOrWhiteSpace(ph?.ClinicNameDari))
                    {
                        var cn = _s.ClinicNameDari;
                        info.Item().ContentFromRightToLeft().Text(text =>
                        {
                            if      (cn.TextAlign == "Center") text.AlignCenter();
                            else if (cn.TextAlign == "Left")   text.AlignLeft();
                            else                               text.AlignRight();
                            var sp = text.Span(ph.ClinicNameDari)
                                .FontFamily(cn.FontFamily).FontSize(cn.FontSize)
                                .FontColor(Clr(cn.FontColor));
                            if (cn.Bold)   sp.Bold();
                            if (cn.Italic) sp.Italic();
                        });
                    }

                    info.Item().ContentFromRightToLeft().Text(text =>
                    {
                        if      (docDar.TextAlign == "Center") text.AlignCenter();
                        else if (docDar.TextAlign == "Left")   text.AlignLeft();
                        else                                   text.AlignRight();
                        var sp = text.Span(ph?.NameDari ?? "کلینیک سرپایی")
                            .FontFamily(docDar.FontFamily).FontSize(docDar.FontSize)
                            .FontColor(Clr(docDar.FontColor));
                        if (docDar.Bold)   sp.Bold();
                        if (docDar.Italic) sp.Italic();
                    });

                    if (!string.IsNullOrWhiteSpace(ph?.SpecialityDari))
                        info.Item().PaddingTop(2).ContentFromRightToLeft().Text(text =>
                        {
                            if      (spDar.TextAlign == "Center") text.AlignCenter();
                            else if (spDar.TextAlign == "Left")   text.AlignLeft();
                            else                                   text.AlignRight();
                            var sp = text.Span(ph.SpecialityDari)
                                .FontFamily(spDar.FontFamily).FontSize(spDar.FontSize)
                                .FontColor(Clr(spDar.FontColor));
                            if (spDar.Bold)   sp.Bold();
                            if (spDar.Italic) sp.Italic();
                        });

                    if (!string.IsNullOrWhiteSpace(ph?.OtherSpecialityDari))
                        info.Item().PaddingTop(1).ContentFromRightToLeft().Text(text =>
                        {
                            if      (otSpDar.TextAlign == "Center") text.AlignCenter();
                            else if (otSpDar.TextAlign == "Left")   text.AlignLeft();
                            else                                     text.AlignRight();
                            var sp = text.Span(ph.OtherSpecialityDari)
                                .FontFamily(otSpDar.FontFamily).FontSize(otSpDar.FontSize)
                                .FontColor(Clr(otSpDar.FontColor));
                            if (otSpDar.Bold)   sp.Bold();
                            if (otSpDar.Italic) sp.Italic();
                        });
                });
            });

            // Divider line
            if (_s.DividerVisible)
            {
                var dt = (float)_s.DividerThickness;
                var dc = Clr(_s.DividerColor);
                switch (_s.DividerStyle)
                {
                    case "Double":
                        col.Item().PaddingTop(8).BorderBottom(dt).BorderColor(dc);
                        col.Item().PaddingTop(3).BorderBottom(dt).BorderColor(dc);
                        break;
                    case "Dashed":
                        // 8pt dash · 4pt gap · ~42 repeats across A4 content width
                        col.Item().PaddingTop(8).Height(dt).Row(row =>
                        {
                            for (int i = 0; i < 40; i++)
                            {
                                row.ConstantItem(8).Background(dc);
                                row.ConstantItem(4);
                            }
                            row.RelativeItem();
                        });
                        break;
                    case "Dotted":
                        // 2pt dot · 3pt gap · ~99 repeats across A4 content width
                        col.Item().PaddingTop(8).Height(dt).Row(row =>
                        {
                            for (int i = 0; i < 97; i++)
                            {
                                row.ConstantItem(2).Background(dc);
                                row.ConstantItem(3);
                            }
                            row.RelativeItem();
                        });
                        break;
                    default: // Solid
                        col.Item().PaddingTop(8).BorderBottom(dt).BorderColor(dc);
                        break;
                }
            }
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
        var v   = data.Visit;
        var pb  = S_PBar;
        var pid = _s.PatientId;
        float fs = pb.FontSize;

        var container = c.Background(Clr(pb.BackgroundColor));
        if (pb.ShowBorder)
            container = container.BorderLeft(pb.BorderThickness).BorderColor(Clr(pb.BorderColor));

        container.Padding(pb.Padding).Column(col =>
        {
            // ── Row 1 (RTL order): Date | Sex | Age | Name ───────────────────
            // Row items are added left→right; ContentFromRightToLeft() is NOT used
            // on the outer row, so first item = leftmost. Visually right-to-left:
            // Name (rightmost) | Age | Sex | Date (leftmost).
            col.Item().Row(row =>
            {
                // Date — leftmost in PDF = rightmost when read RTL
                row.RelativeItem(3).Element(e =>
                    e.ContentFromRightToLeft().Column(dc =>
                    {
                        dc.Item().Text(text =>
                        {
                            text.Span("تاریخ:  ").Bold().FontFamily(pb.FontFamily).FontSize(fs);
                            text.Span(v.HijriDate ?? v.OpdDate?.ToString("yyyy-MM-dd") ?? "—")
                                .FontFamily(pb.FontFamily).FontSize(fs);
                        });
                        if (!string.IsNullOrWhiteSpace(v.NextVisitDate))
                            dc.Item().PaddingTop(3).Text(text =>
                            {
                                text.Span("مراجعه بعدی:  ").Bold().FontFamily(pb.FontFamily).FontSize(fs - 1);
                                text.Span(v.NextVisitDate).FontFamily(pb.FontFamily).FontSize(fs - 1)
                                    .FontColor(Clr("#1565C0"));
                            });
                    }));

                // Sex
                row.RelativeItem(2).AlignCenter().Element(e =>
                    e.ContentFromRightToLeft().Text(text =>
                    {
                        text.Span("جنسیت:  ").Bold().FontFamily(pb.FontFamily).FontSize(fs);
                        text.Span(TranslateSex(CleanFieldValue(p.Sex)))
                            .FontFamily(pb.FontFamily).FontSize(fs);
                    }));

                // Age
                row.RelativeItem(2).AlignCenter().Element(e =>
                    e.ContentFromRightToLeft().Text(text =>
                    {
                        text.Span("سن:  ").Bold().FontFamily(pb.FontFamily).FontSize(fs);
                        text.Span(v.Age.HasValue ? v.Age.Value.ToString() : "—")
                            .FontFamily(pb.FontFamily).FontSize(fs);
                    }));

                // Name — rightmost in PDF = leftmost when read RTL (most prominent)
                row.RelativeItem(3).AlignRight().Element(e =>
                    e.ContentFromRightToLeft().Text(text =>
                    {
                        text.Span("نام مریض:  ").Bold().FontFamily(pb.FontFamily).FontSize(fs);
                        text.Span(p.PatientName ?? "—").Bold()
                            .FontFamily(pb.FontFamily).FontSize(fs + 1).FontColor(Accent);
                    }));
            });

            // ── Row 2: Patient ID (optional) ──────────────────────────────────
            if (_s.ShowPatientId && !string.IsNullOrWhiteSpace(p.PatientCode))
            {
                col.Item().PaddingTop(3).Element(e =>
                    e.ContentFromRightToLeft().Text(text =>
                    {
                        text.Span("نمبر مسلسل:  ").Bold()
                            .FontFamily(pid.FontFamily).FontSize(pid.FontSize)
                            .FontColor(Clr(pid.FontColor));
                        var sp = text.Span(p.PatientCode)
                            .FontFamily(pid.FontFamily).FontSize(pid.FontSize)
                            .FontColor(Clr(pid.FontColor));
                        if (pid.Bold)   sp.Bold();
                        if (pid.Italic) sp.Italic();
                    }));
            }
        });
    }

    // ── BODY: [Clinical left] | [Rx right] ───────────────────────────────────
    private void ComposeBody(IContainer c)
    {
        var v = data.Visit;
        c.Row(row =>
        {
            // Left: Vital Signs + Clinical Findings + Diagnosis + Lab Tests
            row.RelativeItem(1.4f).Column(col =>
            {
                col.Item().Element(ComposeVitalsBox);
                col.Item().PaddingTop(6).Element(ctx =>
                    ComposeLabelledBox(ctx, "Clinical Findings", v.ClinicalFindings, S_CF));
                col.Item().PaddingTop(6).Element(ctx =>
                    ComposeLabelledBox(ctx, "Diagnosis", v.Diagnosis, S_Dx));

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
        var v   = data.Visit;
        var st  = S_VS;
        float fs = st.FontSize;

        var outer = c;
        if (st.ShowBorder)
            outer = outer.Border(st.BorderThickness).BorderColor(Clr(st.BorderColor));

        outer.Column(col =>
        {
            var titleRow = col.Item().Background(Clr(st.TitleBgColor)).Padding(5);
            var titleText = titleRow.Text("Vital Signs")
                .FontFamily(st.FontFamily)
                .FontSize(Math.Max(fs - 0.5f, 6))
                .FontColor(Clr(st.TitleFontColor));
            if (st.TitleBold) titleText.Bold();

            col.Item().Padding(st.Padding).Column(vs =>
            {
                float gap = Math.Max(st.LabelSpacing, 0f);

                // BP + PR
                if (!string.IsNullOrEmpty(v.BP) || !string.IsNullOrEmpty(v.PR))
                    vs.Item().PaddingBottom(2).Row(r =>
                    {
                        r.AutoItem().Text("BP:").FontFamily(st.FontFamily).FontSize(fs).Bold();
                        r.ConstantItem(gap);
                        r.AutoItem().Text(v.BP ?? "—").FontFamily(st.FontFamily).FontSize(fs);
                        r.ConstantItem(18);
                        r.AutoItem().Text("PR:").FontFamily(st.FontFamily).FontSize(fs).Bold();
                        r.ConstantItem(gap);
                        r.AutoItem().Text(v.PR ?? "—").FontFamily(st.FontFamily).FontSize(fs);
                    });

                // RR
                if (!string.IsNullOrEmpty(v.RR))
                    vs.Item().PaddingTop(6).Row(r =>
                    {
                        r.AutoItem().Text("RR:").FontFamily(st.FontFamily).FontSize(fs).Bold();
                        r.ConstantItem(gap);
                        r.AutoItem().Text(v.RR).FontFamily(st.FontFamily).FontSize(fs);
                    });

                // BT + BW
                if (!string.IsNullOrEmpty(v.BT) || !string.IsNullOrEmpty(v.BW))
                    vs.Item().PaddingTop(6).Row(r =>
                    {
                        r.AutoItem().Text("BT:").FontFamily(st.FontFamily).FontSize(fs).Bold();
                        r.ConstantItem(gap);
                        r.AutoItem().Text(v.BT ?? "—").FontFamily(st.FontFamily).FontSize(fs);
                        r.ConstantItem(18);
                        r.AutoItem().Text("BW:").FontFamily(st.FontFamily).FontSize(fs).Bold();
                        r.ConstantItem(gap);
                        r.AutoItem().Text(v.BW ?? "—").FontFamily(st.FontFamily).FontSize(fs);
                    });

                var anyVital = !string.IsNullOrEmpty(v.BP) || !string.IsNullOrEmpty(v.PR)
                            || !string.IsNullOrEmpty(v.RR) || !string.IsNullOrEmpty(v.BT)
                            || !string.IsNullOrEmpty(v.BW);
                if (!anyVital)
                    vs.Item().Text("—").FontFamily(st.FontFamily)
                             .FontSize(Math.Max(fs - 0.5f, 6)).FontColor(Gray).Italic();
            });
        });
    }

    // ── LABELLED BOX (Clinical Findings / Diagnosis / Lab Tests) ─────────────
    private void ComposeLabelledBox(IContainer c, string title, string? content, SectionStyle st)
    {
        float fs = st.FontSize;

        var outer = c;
        if (st.ShowBorder)
            outer = outer.Border(st.BorderThickness).BorderColor(Clr(st.BorderColor));

        outer.Column(col =>
        {
            var titleRow  = col.Item().Background(Clr(st.TitleBgColor)).Padding(5);
            var titleText = titleRow.Text(title)
                .FontFamily(st.FontFamily)
                .FontSize(Math.Max(fs - 0.5f, 6))
                .FontColor(Clr(st.TitleFontColor));
            if (st.TitleBold) titleText.Bold();

            var body = string.IsNullOrWhiteSpace(content) ? "—" : content;
            col.Item().Padding(st.Padding).Text(text =>
            {
                if      (st.TextAlign == "Center") text.AlignCenter();
                else if (st.TextAlign == "Right")  text.AlignRight();
                text.Span(body).FontFamily(st.FontFamily).FontSize(fs).FontColor(Clr(st.FontColor));
            });
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

        var groups = data.LabTests
            .GroupBy(t => string.IsNullOrWhiteSpace(t.Category) ? "General" : t.Category)
            .OrderBy(g => g.Key)
            .ToList();

        bool multiGroup = groups.Count > 1;

        outer.Column(col =>
        {
            var titleRow  = col.Item().Background(Clr(st.TitleBgColor)).Padding(5);
            var titleText = titleRow.Text("Lab Tests")
                .FontFamily(st.FontFamily)
                .FontSize(Math.Max(fs - 0.5f, 6))
                .FontColor(Clr(st.TitleFontColor));
            if (st.TitleBold) titleText.Bold();

            col.Item().Padding(st.Padding).Column(body =>
            {
                foreach (var grp in groups)
                {
                    if (multiGroup)
                    {
                        body.Item().PaddingBottom(3).Text(text =>
                        {
                            if      (st.TextAlign == "Center") text.AlignCenter();
                            else if (st.TextAlign == "Right")  text.AlignRight();
                            text.Span(grp.Key)
                                .FontFamily(st.FontFamily)
                                .FontSize(Math.Max(fs - 0.5f, 6))
                                .FontColor(Clr(st.FontColor))
                                .Bold();
                        });
                    }

                    foreach (var t in grp)
                    {
                        var label = string.IsNullOrEmpty(t.Abbreviation)
                            ? t.TestName ?? ""
                            : $"{t.TestName} ({t.Abbreviation})";

                        body.Item()
                            .PaddingLeft(multiGroup ? 10 : 0)
                            .PaddingBottom(3)
                            .Text(text =>
                            {
                                if      (st.TextAlign == "Center") text.AlignCenter();
                                else if (st.TextAlign == "Right")  text.AlignRight();
                                text.Span($"•  {label}").FontFamily(st.FontFamily).FontSize(fs).FontColor(Clr(st.FontColor));
                            });
                    }

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
            col.Item()
               .Text("Rx")
               .FontSize(24).Bold().Italic().FontColor(Accent);

            col.Item().PaddingTop(6).Element(ComposeRxLines);
        });
    }

    private void ComposeRxLines(IContainer c)
    {
        var st  = S_Rx;
        float fs = st.FontSize;

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
                       entry.Item().Row(row =>
                       {
                           row.ConstantItem(24)
                              .Text($"{i + 1} )")
                              .FontFamily(st.FontFamily).FontSize(fs).Bold().FontColor(Accent);

                           row.ConstantItem(58)
                              .Text(line.Type ?? "")
                              .FontFamily(st.FontFamily).FontSize(fs).FontColor(Gray);

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

                           if (!string.IsNullOrEmpty(line.Usage))
                               row.RelativeItem().Element(e =>
                                   e.AlignRight().ContentFromRightToLeft()
                                    .Text(line.Usage)
                                    .FontFamily(st.FontFamily)
                                    .FontSize(Math.Max(fs - 1.5f, 6))
                                    .FontColor(Clr(st.FontColor)));
                       });

                       if (line.Qty.HasValue)
                           entry.Item().PaddingTop(2).PaddingLeft(82)
                                .Text($"N = {line.Qty}")
                                .FontFamily(st.FontFamily)
                                .FontSize(Math.Max(fs - 1.5f, 6)).FontColor(Gray);

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
        float fs = st.FontSize;

        string imgDir     = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "image");
        byte[]? phoneIcon = TryLoadImage(Path.Combine(imgDir, "PhoneNumber.png"));
        byte[]? waIcon    = TryLoadImage(Path.Combine(imgDir, "WhatsApp.png"));

        c.Column(mainCol =>
        {
            if (_s.FooterDividerVisible)
            {
                var dt = (float)_s.FooterDividerThickness;
                var dc = Clr(_s.FooterDividerColor);
                switch (_s.FooterDividerStyle)
                {
                    case "Double":
                        mainCol.Item().BorderBottom(dt).BorderColor(dc);
                        mainCol.Item().PaddingTop(3).BorderBottom(dt).BorderColor(dc);
                        break;
                    case "Dashed":
                        mainCol.Item().Height(dt).Row(row =>
                        {
                            for (int i = 0; i < 40; i++) { row.ConstantItem(8).Background(dc); row.ConstantItem(4); }
                            row.RelativeItem();
                        }); break;
                    case "Dotted":
                        mainCol.Item().Height(dt).Row(row =>
                        {
                            for (int i = 0; i < 97; i++) { row.ConstantItem(2).Background(dc); row.ConstantItem(3); }
                            row.RelativeItem();
                        }); break;
                    default: // Solid
                        mainCol.Item().BorderBottom(dt).BorderColor(dc); break;
                }
            }

            mainCol.Item().PaddingTop(st.Padding).Column(col =>
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
        }); // inner col
        }); // mainCol
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static byte[]? TryLoadImage(string path)
    {
        try { return File.Exists(path) ? File.ReadAllBytes(path) : null; }
        catch { return null; }
    }

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
