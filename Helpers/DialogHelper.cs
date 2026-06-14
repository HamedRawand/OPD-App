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
    ///
    /// • NoResize windows (fixed dialogs / SizeToContent): MaxWidth/MaxHeight are capped
    ///   at 95% × 92% of the work area so content can never overflow the screen.
    ///
    /// • CanResize / CanResizeWithGrip windows: no MaxWidth/MaxHeight is set — the window
    ///   can grow freely to full screen, exactly like the main application window.
    /// </summary>
    public static void ApplyConstraints(Window window)
    {
        if (window.ResizeMode == ResizeMode.NoResize)
        {
            var wa = SystemParameters.WorkArea;
            window.MaxWidth  = Math.Max(360, wa.Width  * 0.95);
            window.MaxHeight = Math.Max(300, wa.Height * 0.92);
        }
        // Resizable windows: MaxWidth/MaxHeight left at WPF default (unlimited).
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
