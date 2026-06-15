using System.Windows;
using System.Windows.Controls;
using OPDClinic.ViewModels;

namespace OPDClinic.Views;

public partial class ReportDesignView : UserControl
{
    public ReportDesignViewModel ViewModel { get; }

    public ReportDesignView()
    {
        InitializeComponent();
        ViewModel = new ReportDesignViewModel();
        DataContext = ViewModel;
        ViewModel.Load();
    }

    // ── Color swatch click — opens Windows color picker ───────────────────────
    private void ColorSwatch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        var propName = btn.Tag?.ToString();
        if (string.IsNullOrEmpty(propName)) return;

        var vm = btn.DataContext as SectionStyleVm;
        if (vm is null) return;

        var prop = typeof(SectionStyleVm).GetProperty(propName);
        if (prop is null) return;

        var currentHex = prop.GetValue(vm)?.ToString() ?? "#000000";

        using var dialog = new System.Windows.Forms.ColorDialog
        {
            FullOpen  = true,
            Color     = HexToDrawingColor(currentHex)
        };

        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var c = dialog.Color;
            prop.SetValue(vm, $"#{c.R:X2}{c.G:X2}{c.B:X2}");
        }
    }

    // ── Divider color swatch click ────────────────────────────────────────────
    private void DividerColorSwatch_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.ColorDialog
        {
            FullOpen = true,
            Color    = HexToDrawingColor(ViewModel.DividerColor)
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var c = dialog.Color;
            ViewModel.DividerColor = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }
    }

    // ── Footer divider color swatch click ────────────────────────────────────
    private void FooterDividerColorSwatch_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.ColorDialog
        {
            FullOpen = true,
            Color    = HexToDrawingColor(ViewModel.FooterDividerColor)
        };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            var c = dialog.Color;
            ViewModel.FooterDividerColor = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        }
    }

    private static System.Drawing.Color HexToDrawingColor(string hex)
    {
        try
        {
            var c = (System.Windows.Media.Color)
                    System.Windows.Media.ColorConverter.ConvertFromString(hex);
            return System.Drawing.Color.FromArgb(c.R, c.G, c.B);
        }
        catch { return System.Drawing.Color.Black; }
    }
}
