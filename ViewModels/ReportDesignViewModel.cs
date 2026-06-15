using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OPDClinic.Models;
using OPDClinic.Services;

namespace OPDClinic.ViewModels;

// ════════════════════════════════════════════════════════════════════════════════
//  SectionStyleVm — observable wrapper around SectionStyle
// ════════════════════════════════════════════════════════════════════════════════
public partial class SectionStyleVm : ObservableObject
{
    private readonly Func<SectionStyle> _defaults;
    private static int _instanceCounter;

    /// <summary>Unique per-instance group name so RadioButtons in one section
    /// don't interfere with RadioButtons in another section's DataTemplate.</summary>
    public string AlignGroupName { get; } = $"Align_{System.Threading.Interlocked.Increment(ref _instanceCounter)}";

    public SectionStyleVm(Func<SectionStyle> defaults)
    {
        _defaults = defaults;
        LoadFrom(defaults());
    }

    // ── Font ──────────────────────────────────────────────────────────────────
    [ObservableProperty][NotifyPropertyChangedFor(nameof(FontFamilyWpf))]
    private string _fontFamily = "Calibri";

    [ObservableProperty][NotifyPropertyChangedFor(nameof(FontSizeWpf))]
    private double _fontSize = 9;

    [ObservableProperty][NotifyPropertyChangedFor(nameof(FontWeightWpf))]
    private bool _bold;

    [ObservableProperty][NotifyPropertyChangedFor(nameof(FontStyleWpf))]
    private bool _italic;

    [ObservableProperty][NotifyPropertyChangedFor(nameof(FontColorBrush))]
    private string _fontColor = "#111111";

    // ── Appearance ────────────────────────────────────────────────────────────
    [ObservableProperty][NotifyPropertyChangedFor(nameof(BgBrush))]
    private string _backgroundColor = "#FFFFFF";

    [ObservableProperty][NotifyPropertyChangedFor(nameof(PaddingThicknessWpf))]
    private double _padding = 7;

    // ── Border ────────────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BorderThicknessWpf))]
    [NotifyPropertyChangedFor(nameof(BorderVisibility))]
    private bool _showBorder = true;

    [ObservableProperty][NotifyPropertyChangedFor(nameof(BorderThicknessWpf))]
    private double _borderThickness = 1;

    [ObservableProperty][NotifyPropertyChangedFor(nameof(BorderColorBrush))]
    private string _borderColor = "#999999";

    // ── Section title bar ─────────────────────────────────────────────────────
    [ObservableProperty][NotifyPropertyChangedFor(nameof(TitleFontBrush))]
    private string _titleFontColor = "#1565C0";

    [ObservableProperty][NotifyPropertyChangedFor(nameof(TitleBgBrush))]
    private string _titleBgColor = "#E4E4E4";

    [ObservableProperty][NotifyPropertyChangedFor(nameof(TitleFontWeightWpf))]
    private bool _titleBold = true;

    // ── Text alignment ────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsAlignLeft), nameof(IsAlignCenter), nameof(IsAlignRight), nameof(TextAlignmentWpf), nameof(TextAlignmentWpfRtl))]
    private string _textAlign = "Left";

    public bool IsAlignLeft
    {
        get => TextAlign == "Left";
        set { if (value) TextAlign = "Left"; }
    }
    public bool IsAlignCenter
    {
        get => TextAlign == "Center";
        set { if (value) TextAlign = "Center"; }
    }
    public bool IsAlignRight
    {
        get => TextAlign == "Right";
        set { if (value) TextAlign = "Right"; }
    }

    // ── Label spacing (Vital Signs only) ──────────────────────────────────────
    [ObservableProperty]
    private double _labelSpacing = 8;

    // ── Computed WPF properties (used by live preview bindings) ───────────────
    public FontFamily FontFamilyWpf      => SafeFont(FontFamily);
    public double     FontSizeWpf        => Math.Clamp(FontSize, 6, 36);
    public FontWeight FontWeightWpf      => Bold ? FontWeights.Bold : FontWeights.Normal;
    public FontStyle  FontStyleWpf       => Italic ? FontStyles.Italic : FontStyles.Normal;
    public FontWeight TitleFontWeightWpf => TitleBold ? FontWeights.Bold : FontWeights.Normal;
    public Brush      FontColorBrush     => SafeBrush(FontColor,     Colors.Black);
    public Brush      BgBrush            => SafeBrush(BackgroundColor, Colors.White);
    public Brush      BorderColorBrush   => SafeBrush(BorderColor,   Colors.Gray);
    public Brush      TitleFontBrush     => SafeBrush(TitleFontColor, Colors.DarkBlue);
    public Brush      TitleBgBrush       => SafeBrush(TitleBgColor,  Colors.LightGray);
    public Thickness     BorderThicknessWpf  => ShowBorder ? new Thickness(BorderThickness) : new Thickness(0);
    public Visibility    BorderVisibility    => ShowBorder ? Visibility.Visible : Visibility.Collapsed;
    public Thickness     PaddingThicknessWpf  => new Thickness(Padding);
    public TextAlignment TextAlignmentWpf    => TextAlign switch { "Center" => TextAlignment.Center, "Right" => TextAlignment.Right, _ => TextAlignment.Left };
    // RTL columns (FlowDirection=RightToLeft): Left/Right are logically inverted in WPF, so swap them
    // so the user's "Right" selection still means physically right-aligned text.
    public TextAlignment TextAlignmentWpfRtl => TextAlign switch { "Center" => TextAlignment.Center, "Right" => TextAlignment.Left, _ => TextAlignment.Right };

    // ── Commands ──────────────────────────────────────────────────────────────
    [RelayCommand]
    public void Reset() => LoadFrom(_defaults());

    // ── Serialization ─────────────────────────────────────────────────────────
    public void LoadFrom(SectionStyle s)
    {
        FontFamily      = s.FontFamily;
        FontSize        = s.FontSize;
        Bold            = s.Bold;
        Italic          = s.Italic;
        FontColor       = s.FontColor;
        BackgroundColor = s.BackgroundColor;
        Padding         = s.Padding;
        ShowBorder      = s.ShowBorder;
        BorderThickness = s.BorderThickness;
        BorderColor     = s.BorderColor;
        TitleFontColor  = s.TitleFontColor;
        TitleBgColor    = s.TitleBgColor;
        TitleBold       = s.TitleBold;
        TextAlign       = s.TextAlign;
        LabelSpacing    = s.LabelSpacing;
    }

    public SectionStyle ToModel() => new()
    {
        FontFamily      = FontFamily,
        FontSize        = (float)FontSize,
        Bold            = Bold,
        Italic          = Italic,
        FontColor       = FontColor,
        BackgroundColor = BackgroundColor,
        Padding         = (float)Padding,
        ShowBorder      = ShowBorder,
        BorderThickness = (float)BorderThickness,
        BorderColor     = BorderColor,
        TitleFontColor  = TitleFontColor,
        TitleBgColor    = TitleBgColor,
        TitleBold       = TitleBold,
        TextAlign       = TextAlign,
        LabelSpacing    = (float)LabelSpacing
    };

    // ── Static lists for XAML ─────────────────────────────────────────────────
    public static List<string> AvailableFonts { get; } = new()
    {
        "Calibri", "Segoe UI", "Arial", "Tahoma",
        "Times New Roman", "Noto Naskh Arabic", "Vazirmatn"
    };

    public static List<double> CommonSizes { get; } = new()
    {
        7, 7.5, 8, 8.5, 9, 9.5, 10, 10.5, 11, 12, 13, 14, 16, 17, 18, 20
    };

    // ── Helpers ───────────────────────────────────────────────────────────────
    private static Brush SafeBrush(string hex, Color fallback)
    {
        try
        {
            var c = (Color)ColorConverter.ConvertFromString(hex);
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }
        catch { return new SolidColorBrush(fallback); }
    }

    private static FontFamily SafeFont(string name)
    {
        try { return new FontFamily(name); }
        catch { return new FontFamily("Calibri"); }
    }
}


// ════════════════════════════════════════════════════════════════════════════════
//  ReportDesignViewModel — top-level VM for the Report Design settings tab
// ════════════════════════════════════════════════════════════════════════════════
public partial class ReportDesignViewModel : ObservableObject
{
    // ── Section view-models ───────────────────────────────────────────────────
    public SectionStyleVm ClinicNameEn        { get; } = new(SectionStyle.MakeClinicName);
    public SectionStyleVm ClinicNameDari      { get; } = new(SectionStyle.MakeClinicNameDari);
    public SectionStyleVm Tagline             { get; } = new(SectionStyle.MakeTagline);
    public SectionStyleVm DoctorNameEn        { get; } = new(SectionStyle.MakeDoctorNameEn);
    public SectionStyleVm SpecialityEn        { get; } = new(SectionStyle.MakeSpecialityEn);
    public SectionStyleVm OtherSpecialityEn   { get; } = new(SectionStyle.MakeOtherSpecialityEn);
    public SectionStyleVm DoctorNameDari      { get; } = new(SectionStyle.MakeDoctorNameDari);
    public SectionStyleVm SpecialityDari      { get; } = new(SectionStyle.MakeSpecialityDari);
    public SectionStyleVm OtherSpecialityDari { get; } = new(SectionStyle.MakeOtherSpecialityDari);
    public SectionStyleVm PatientBar          { get; } = new(SectionStyle.MakePatientBar);
    public SectionStyleVm PatientId           { get; } = new(SectionStyle.MakePatientId);
    public SectionStyleVm VitalSigns       { get; } = new(SectionStyle.MakeBox);
    public SectionStyleVm ClinicalFindings { get; } = new(SectionStyle.MakeBox);
    public SectionStyleVm Diagnosis        { get; } = new(SectionStyle.MakeBox);
    public SectionStyleVm LabTests         { get; } = new(SectionStyle.MakeBox);
    public SectionStyleVm RxSection        { get; } = new(SectionStyle.MakeRx);
    public SectionStyleVm Footer           { get; } = new(SectionStyle.MakeFooter);

    // ── Global ────────────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _blackAndWhiteMode;
    [ObservableProperty] private string _globalFontFamily = "Calibri";
    [ObservableProperty] private double _logoSize = 54;
    [ObservableProperty] private double _patientBarGap = 4;
    [ObservableProperty] private bool   _showPatientId = true;

    // ── Header divider ────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DividerThicknessWpf))]
    private bool _dividerVisible = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DividerThicknessWpf))]
    private double _dividerThickness = 2.5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DividerBrush))]
    private string _dividerColor = "#1565C0";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DividerDashArrayWpf), nameof(DividerIsSingle), nameof(DividerIsDouble))]
    private string _dividerStyle = "Solid";

    public Brush             DividerBrush        => SafeBrush(DividerColor, Colors.DarkBlue);
    public Thickness         DividerThicknessWpf => DividerVisible ? new Thickness(0, DividerThickness, 0, 0) : new Thickness(0);
    public DoubleCollection? DividerDashArrayWpf => DividerStyle switch {
        "Dashed" => new DoubleCollection { 6, 3 },
        "Dotted" => new DoubleCollection { 1.5, 2 },
        _        => null
    };
    public bool DividerIsSingle => DividerStyle != "Double";
    public bool DividerIsDouble => DividerStyle == "Double";

    // ── Footer divider ────────────────────────────────────────────────────────
    [ObservableProperty]
    private bool _footerDividerVisible = true;

    [ObservableProperty]
    private double _footerDividerThickness = 1.5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FooterDividerBrush))]
    private string _footerDividerColor = "#1565C0";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FooterDividerDashArrayWpf), nameof(FooterDividerIsSingle), nameof(FooterDividerIsDouble))]
    private string _footerDividerStyle = "Solid";

    public Brush             FooterDividerBrush        => SafeBrush(FooterDividerColor, Colors.DarkBlue);
    public DoubleCollection? FooterDividerDashArrayWpf => FooterDividerStyle switch {
        "Dashed" => new DoubleCollection { 6, 3 },
        "Dotted" => new DoubleCollection { 1.5, 2 },
        _        => null
    };
    public bool FooterDividerIsSingle => FooterDividerStyle != "Double";
    public bool FooterDividerIsDouble => FooterDividerStyle == "Double";

    public static List<string> DividerStyleOptions { get; } = ["Solid", "Dashed", "Dotted", "Double"];

    private static Brush SafeBrush(string hex, Color fallback)
    {
        try
        {
            var c = (Color)ColorConverter.ConvertFromString(hex);
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }
        catch { return new SolidColorBrush(fallback); }
    }

    // ── Status bar ────────────────────────────────────────────────────────────
    [ObservableProperty] private string _statusMessage  = "";
    [ObservableProperty] private bool   _statusIsError;
    [ObservableProperty] private bool   _statusVisible;

    // ── Load from service ─────────────────────────────────────────────────────
    public void Load()
    {
        var s = ReportSettingsService.Current;
        BlackAndWhiteMode = s.BlackAndWhiteMode;
        GlobalFontFamily  = s.GlobalFontFamily;
        LogoSize          = s.LogoSize;
        PatientBarGap     = s.PatientBarGap;
        ShowPatientId     = s.ShowPatientId;
        DividerVisible    = s.DividerVisible;
        DividerThickness  = s.DividerThickness;
        DividerColor      = s.DividerColor;
        DividerStyle      = s.DividerStyle;
        FooterDividerVisible    = s.FooterDividerVisible;
        FooterDividerThickness  = s.FooterDividerThickness;
        FooterDividerColor      = s.FooterDividerColor;
        FooterDividerStyle      = s.FooterDividerStyle;
        ClinicNameEn        .LoadFrom(s.ClinicNameEn);
        ClinicNameDari      .LoadFrom(s.ClinicNameDari);
        Tagline             .LoadFrom(s.Tagline);
        DoctorNameEn        .LoadFrom(s.DoctorNameEn);
        SpecialityEn        .LoadFrom(s.SpecialityEn);
        OtherSpecialityEn   .LoadFrom(s.OtherSpecialityEn);
        DoctorNameDari      .LoadFrom(s.DoctorNameDari);
        SpecialityDari      .LoadFrom(s.SpecialityDari);
        OtherSpecialityDari .LoadFrom(s.OtherSpecialityDari);
        PatientBar          .LoadFrom(s.PatientBar);
        PatientId           .LoadFrom(s.PatientId);
        VitalSigns       .LoadFrom(s.VitalSigns);
        ClinicalFindings .LoadFrom(s.ClinicalFindings);
        Diagnosis        .LoadFrom(s.Diagnosis);
        LabTests         .LoadFrom(s.LabTests);
        RxSection        .LoadFrom(s.RxSection);
        Footer           .LoadFrom(s.Footer);
    }

    // ── Commands ──────────────────────────────────────────────────────────────
    [RelayCommand]
    public void Save()
    {
        ReportSettingsService.Save(ToModel());
        ShowStatus("✓  Report design settings saved successfully.", false);
    }

    [RelayCommand]
    public void ResetAll()
    {
        var def = new ReportSettings();
        BlackAndWhiteMode = def.BlackAndWhiteMode;
        GlobalFontFamily  = def.GlobalFontFamily;
        LogoSize          = def.LogoSize;
        PatientBarGap     = def.PatientBarGap;
        ShowPatientId     = def.ShowPatientId;
        DividerVisible    = def.DividerVisible;
        DividerThickness  = def.DividerThickness;
        DividerColor      = def.DividerColor;
        DividerStyle      = def.DividerStyle;
        FooterDividerVisible    = def.FooterDividerVisible;
        FooterDividerThickness  = def.FooterDividerThickness;
        FooterDividerColor      = def.FooterDividerColor;
        FooterDividerStyle      = def.FooterDividerStyle;
        ClinicNameEn        .LoadFrom(def.ClinicNameEn);
        ClinicNameDari      .LoadFrom(def.ClinicNameDari);
        Tagline             .LoadFrom(def.Tagline);
        DoctorNameEn        .LoadFrom(def.DoctorNameEn);
        SpecialityEn        .LoadFrom(def.SpecialityEn);
        OtherSpecialityEn   .LoadFrom(def.OtherSpecialityEn);
        DoctorNameDari      .LoadFrom(def.DoctorNameDari);
        SpecialityDari      .LoadFrom(def.SpecialityDari);
        OtherSpecialityDari .LoadFrom(def.OtherSpecialityDari);
        PatientBar          .LoadFrom(def.PatientBar);
        PatientId           .LoadFrom(def.PatientId);
        VitalSigns       .LoadFrom(def.VitalSigns);
        ClinicalFindings .LoadFrom(def.ClinicalFindings);
        Diagnosis        .LoadFrom(def.Diagnosis);
        LabTests         .LoadFrom(def.LabTests);
        RxSection        .LoadFrom(def.RxSection);
        Footer           .LoadFrom(def.Footer);
        ShowStatus("All settings reset to factory defaults.", false);
    }

    [RelayCommand]
    public void ApplyBwPalette()
    {
        // Override colours with grayscale-safe equivalents
        DoctorNameEn.FontColor      = "#000000";
        SpecialityEn.FontColor      = "#000000";
        OtherSpecialityEn.FontColor = "#000000";
        DoctorNameDari.FontColor      = "#000000";
        SpecialityDari.FontColor      = "#000000";
        OtherSpecialityDari.FontColor = "#000000";
        PatientBar.BackgroundColor = "#EBEBEB";
        PatientBar.BorderColor     = "#444444";
        foreach (var s in new[] { VitalSigns, ClinicalFindings, Diagnosis, LabTests })
        {
            s.TitleBgColor   = "#E0E0E0";
            s.TitleFontColor = "#222222";
            s.BorderColor    = "#777777";
        }
        RxSection.FontColor  = "#000000";
        Footer.BorderColor   = "#777777";
        Footer.FontColor     = "#333333";
        ShowStatus("B&W palette applied to all sections. Click Save to persist.", false);
    }

    // ── Build model from VM state ─────────────────────────────────────────────
    private ReportSettings ToModel() => new()
    {
        BlackAndWhiteMode = BlackAndWhiteMode,
        GlobalFontFamily  = GlobalFontFamily,
        LogoSize          = LogoSize,
        PatientBarGap     = PatientBarGap,
        ShowPatientId     = ShowPatientId,
        DividerVisible    = DividerVisible,
        DividerThickness  = DividerThickness,
        DividerColor      = DividerColor,
        DividerStyle      = DividerStyle,
        FooterDividerVisible    = FooterDividerVisible,
        FooterDividerThickness  = FooterDividerThickness,
        FooterDividerColor      = FooterDividerColor,
        FooterDividerStyle      = FooterDividerStyle,
        ClinicNameEn        = ClinicNameEn.ToModel(),
        ClinicNameDari      = ClinicNameDari.ToModel(),
        Tagline             = Tagline.ToModel(),
        DoctorNameEn        = DoctorNameEn.ToModel(),
        SpecialityEn        = SpecialityEn.ToModel(),
        OtherSpecialityEn   = OtherSpecialityEn.ToModel(),
        DoctorNameDari      = DoctorNameDari.ToModel(),
        SpecialityDari      = SpecialityDari.ToModel(),
        OtherSpecialityDari = OtherSpecialityDari.ToModel(),
        PatientBar          = PatientBar.ToModel(),
        PatientId           = PatientId.ToModel(),
        VitalSigns       = VitalSigns.ToModel(),
        ClinicalFindings = ClinicalFindings.ToModel(),
        Diagnosis        = Diagnosis.ToModel(),
        LabTests         = LabTests.ToModel(),
        RxSection        = RxSection.ToModel(),
        Footer           = Footer.ToModel()
    };

    private void ShowStatus(string msg, bool isError)
    {
        StatusMessage = msg;
        StatusIsError = isError;
        StatusVisible = true;
    }
}
