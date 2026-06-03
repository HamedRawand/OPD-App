using System.Windows;

namespace OPDClinic.Helpers;

/// <summary>
/// Applies responsive size constraints to any Window so it never overflows
/// the screen at any DPI or resolution.
/// </summary>
public static class DialogHelper
{
    /// <summary>
    /// Call this from a dialog constructor immediately after InitializeComponent().
    /// Sets MaxWidth / MaxHeight to 95 % / 92 % of the current work area so the
    /// dialog stays within bounds on small or high-DPI screens.
    /// </summary>
    public static void ApplyConstraints(Window window)
    {
        var wa = SystemParameters.WorkArea;
        window.MaxWidth  = Math.Max(360, wa.Width  * 0.95);
        window.MaxHeight = Math.Max(300, wa.Height * 0.92);
    }

    /// <summary>
    /// Returns a sensible MaxHeight for an inner ScrollViewer inside a dialog,
    /// reserving <paramref name="overhead"/> pixels for title, fields, buttons, etc.
    /// </summary>
    public static double InnerScrollHeight(double overhead = 430)
    {
        var wa = SystemParameters.WorkArea;
        return Math.Max(150, wa.Height * 0.92 - overhead);
    }
}
