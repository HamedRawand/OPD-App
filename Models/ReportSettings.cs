namespace OPDClinic.Models;

/// <summary>User-configurable appearance settings for the printed prescription report.
/// Persisted as JSON to %LocalAppData%\OPDClinic\report_settings.json.</summary>
public class ReportSettings
{
    // ── Global ─────────────────────────────────────────────────────────────────
    public bool   BlackAndWhiteMode { get; set; } = false;
    public string GlobalFontFamily  { get; set; } = "Calibri";

    // ── Per-section styles ─────────────────────────────────────────────────────
    public SectionStyle Header           { get; set; } = SectionStyle.MakeHeader();
    public SectionStyle PatientBar       { get; set; } = SectionStyle.MakePatientBar();
    public SectionStyle VitalSigns       { get; set; } = SectionStyle.MakeBox();
    public SectionStyle ClinicalFindings { get; set; } = SectionStyle.MakeBox();
    public SectionStyle Diagnosis        { get; set; } = SectionStyle.MakeBox();
    public SectionStyle LabTests         { get; set; } = SectionStyle.MakeBox();
    public SectionStyle RxSection        { get; set; } = SectionStyle.MakeRx();
    public SectionStyle Footer           { get; set; } = SectionStyle.MakeFooter();
}

/// <summary>Style properties for one section of the prescription report.</summary>
public class SectionStyle
{
    public string FontFamily      { get; set; } = "Calibri";
    public float  FontSize        { get; set; } = 9f;
    public bool   Bold            { get; set; } = false;
    public string FontColor       { get; set; } = "#111111";
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public float  Padding         { get; set; } = 7f;
    public bool   ShowBorder      { get; set; } = true;
    public float  BorderThickness { get; set; } = 1f;
    public string BorderColor     { get; set; } = "#999999";
    // Section title bar (header strip inside a box)
    public string TitleFontColor  { get; set; } = "#1565C0";
    public string TitleBgColor    { get; set; } = "#E4E4E4";
    public bool   TitleBold       { get; set; } = true;

    public SectionStyle Clone() => (SectionStyle)MemberwiseClone();

    // ── Default factories ──────────────────────────────────────────────────────
    public static SectionStyle MakeHeader() => new()
    {
        FontSize = 17f, Bold = true,
        FontColor = "#1565C0", BackgroundColor = "#FFFFFF",
        ShowBorder = false, Padding = 0f,
        TitleFontColor = "#1565C0", TitleBgColor = "#FFFFFF"
    };

    public static SectionStyle MakePatientBar() => new()
    {
        FontSize = 9f, FontColor = "#111111",
        BackgroundColor = "#EBEBEB",
        ShowBorder = true, BorderColor = "#1565C0",
        BorderThickness = 3.5f, Padding = 8f
    };

    public static SectionStyle MakeBox() => new()
    {
        FontSize = 9f, FontColor = "#111111",
        BackgroundColor = "#FFFFFF",
        ShowBorder = true, BorderColor = "#999999",
        BorderThickness = 1f, Padding = 7f,
        TitleFontColor = "#1565C0", TitleBgColor = "#E4E4E4", TitleBold = true
    };

    public static SectionStyle MakeRx() => new()
    {
        FontSize = 10.5f, Bold = true,
        FontColor = "#111111", BackgroundColor = "#FFFFFF",
        ShowBorder = false, Padding = 4f
    };

    public static SectionStyle MakeFooter() => new()
    {
        FontSize = 8f, FontColor = "#111111",
        BackgroundColor = "#FFFFFF",
        ShowBorder = true, BorderColor = "#999999",
        BorderThickness = 1.5f, Padding = 5f
    };
}
