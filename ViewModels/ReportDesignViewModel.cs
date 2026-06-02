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

    [ObservableProperty]
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
    public Thickness  BorderThicknessWpf => ShowBorder ? new Thickness(BorderThickness) : new Thickness(0);
    public Visibility BorderVisibility   => ShowBorder ? Visibility.Visible : Visibility.Collapsed;

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
        TitleBold       = TitleBold
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
    public SectionStyleVm ClinicName       { get; } = new(SectionStyle.MakeClinicName);
    public SectionStyleVm Tagline          { get; } = new(SectionStyle.MakeTagline);
    public SectionStyleVm Header           { get; } = new(SectionStyle.MakeHeader);
    public SectionStyleVm PatientBar       { get; } = new(SectionStyle.MakePatientBar);
    public SectionStyleVm VitalSigns       { get; } = new(SectionStyle.MakeBox);
    public SectionStyleVm ClinicalFindings { get; } = new(SectionStyle.MakeBox);
    public SectionStyleVm Diagnosis        { get; } = new(SectionStyle.MakeBox);
    public SectionStyleVm LabTests         { get; } = new(SectionStyle.MakeBox);
    public SectionStyleVm RxSection        { get; } = new(SectionStyle.MakeRx);
    public SectionStyleVm Footer           { get; } = new(SectionStyle.MakeFooter);

    // ── Global ────────────────────────────────────────────────────────────────
    [ObservableProperty] private bool   _blackAndWhiteMode;
    [ObservableProperty] private string _globalFontFamily = "Calibri";

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
        ClinicName       .LoadFrom(s.ClinicName);
        Tagline          .LoadFrom(s.Tagline);
        Header           .LoadFrom(s.Header);
        PatientBar       .LoadFrom(s.PatientBar);
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
        ClinicName       .LoadFrom(def.ClinicName);
        Tagline          .LoadFrom(def.Tagline);
        Header           .LoadFrom(def.Header);
        PatientBar       .LoadFrom(def.PatientBar);
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
        Header.FontColor           = "#000000";
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
        ClinicName       = ClinicName.ToModel(),
        Tagline          = Tagline.ToModel(),
        Header           = Header.ToModel(),
        PatientBar       = PatientBar.ToModel(),
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
