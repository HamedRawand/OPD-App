using System.Text.Json.Serialization;

namespace OPDClinic.Models;

/// <summary>User-configurable appearance settings for the printed prescription report.
/// Persisted as JSON to %LocalAppData%\OPDClinic\report_settings.json.</summary>
public class ReportSettings
{
    // ── Global ─────────────────────────────────────────────────────────────────
    public bool   BlackAndWhiteMode  { get; set; } = false;
    public string GlobalFontFamily   { get; set; } = "Calibri";
    public double LogoSize           { get; set; } = 54;
    public bool   DividerVisible     { get; set; } = true;
    public double DividerThickness   { get; set; } = 2.5;
    public string DividerColor       { get; set; } = "#1565C0";
    public string DividerStyle       { get; set; } = "Solid";
    public bool   FooterDividerVisible   { get; set; } = true;
    public double FooterDividerThickness { get; set; } = 1.5;
    public string FooterDividerColor     { get; set; } = "#1565C0";
    public string FooterDividerStyle     { get; set; } = "Solid";
    public double PatientBarGap      { get; set; } = 4;
    public bool   ShowPatientId      { get; set; } = true;

    // ── Per-section styles ─────────────────────────────────────────────────────
    // JsonPropertyName keeps backward-compat with settings files saved before the EN/Dari split.
    [JsonPropertyName("ClinicName")]
    public SectionStyle ClinicNameEn   { get; set; } = SectionStyle.MakeClinicName();
    public SectionStyle ClinicNameDari { get; set; } = SectionStyle.MakeClinicNameDari();
    public SectionStyle Tagline        { get; set; } = SectionStyle.MakeTagline();
    [JsonPropertyName("Header")]
    public SectionStyle DoctorNameEn      { get; set; } = SectionStyle.MakeDoctorNameEn();
    public SectionStyle SpecialityEn      { get; set; } = SectionStyle.MakeSpecialityEn();
    public SectionStyle OtherSpecialityEn { get; set; } = SectionStyle.MakeOtherSpecialityEn();
    [JsonPropertyName("HeaderDari")]
    public SectionStyle DoctorNameDari      { get; set; } = SectionStyle.MakeDoctorNameDari();
    public SectionStyle SpecialityDari      { get; set; } = SectionStyle.MakeSpecialityDari();
    public SectionStyle OtherSpecialityDari { get; set; } = SectionStyle.MakeOtherSpecialityDari();
    public SectionStyle PatientBar     { get; set; } = SectionStyle.MakePatientBar();
    public SectionStyle PatientId      { get; set; } = SectionStyle.MakePatientId();
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
    public bool   Italic          { get; set; } = false;
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
    // Text alignment for content area ("Left" | "Center" | "Right")
    public string TextAlign       { get; set; } = "Left";
    // Vital Signs only: fixed-width gap (pt) between label and value columns
    public float  LabelSpacing    { get; set; } = 8f;

    public SectionStyle Clone() => (SectionStyle)MemberwiseClone();

    // ── Default factories ──────────────────────────────────────────────────────
    public static SectionStyle MakeClinicName() => new()
    {
        FontSize = 20f, Bold = true, Italic = false,
        FontColor = "#1565C0", BackgroundColor = "#FFFFFF",
        ShowBorder = false, Padding = 0f,
        TitleFontColor = "#1565C0", TitleBgColor = "#FFFFFF"
    };

    public static SectionStyle MakeClinicNameDari() => new()
    {
        FontSize = 20f, Bold = true, FontFamily = "Noto Naskh Arabic",
        FontColor = "#1565C0", BackgroundColor = "#FFFFFF",
        ShowBorder = false, Padding = 0f,
        TitleFontColor = "#1565C0", TitleBgColor = "#FFFFFF",
        TextAlign = "Right"
    };

    public static SectionStyle MakeDoctorNameDari() => new()
    {
        FontSize = 17f, Bold = true, FontFamily = "Noto Naskh Arabic",
        FontColor = "#1565C0", BackgroundColor = "#FFFFFF",
        ShowBorder = false, Padding = 0f,
        TitleFontColor = "#1565C0", TitleBgColor = "#FFFFFF",
        TextAlign = "Right"
    };

    public static SectionStyle MakeSpecialityDari() => new()
    {
        FontSize = 10f, Bold = false, FontFamily = "Noto Naskh Arabic",
        FontColor = "#111111", BackgroundColor = "#FFFFFF",
        ShowBorder = false, Padding = 0f,
        TitleFontColor = "#111111", TitleBgColor = "#FFFFFF",
        TextAlign = "Right"
    };

    public static SectionStyle MakeOtherSpecialityDari() => new()
    {
        FontSize = 9f, Bold = false, FontFamily = "Noto Naskh Arabic",
        FontColor = "#777777", BackgroundColor = "#FFFFFF",
        ShowBorder = false, Padding = 0f,
        TitleFontColor = "#777777", TitleBgColor = "#FFFFFF",
        TextAlign = "Right"
    };

    public static SectionStyle MakeTagline() => new()
    {
        FontSize = 9f, Bold = false, Italic = false,
        FontColor = "#555555", BackgroundColor = "#FFFFFF",
        ShowBorder = false, Padding = 0f,
        TitleFontColor = "#555555", TitleBgColor = "#FFFFFF"
    };

    public static SectionStyle MakeDoctorNameEn() => new()
    {
        FontSize = 17f, Bold = true,
        FontColor = "#1565C0", BackgroundColor = "#FFFFFF",
        ShowBorder = false, Padding = 0f,
        TitleFontColor = "#1565C0", TitleBgColor = "#FFFFFF"
    };

    public static SectionStyle MakeSpecialityEn() => new()
    {
        FontSize = 10f, Bold = false,
        FontColor = "#111111", BackgroundColor = "#FFFFFF",
        ShowBorder = false, Padding = 0f,
        TitleFontColor = "#111111", TitleBgColor = "#FFFFFF"
    };

    public static SectionStyle MakeOtherSpecialityEn() => new()
    {
        FontSize = 9f, Bold = false,
        FontColor = "#777777", BackgroundColor = "#FFFFFF",
        ShowBorder = false, Padding = 0f,
        TitleFontColor = "#777777", TitleBgColor = "#FFFFFF"
    };

    public static SectionStyle MakePatientId() => new()
    {
        FontSize = 8f, Bold = false,
        FontColor = "#555555", BackgroundColor = "#FFFFFF",
        ShowBorder = false, Padding = 0f,
        TitleFontColor = "#555555", TitleBgColor = "#FFFFFF"
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
