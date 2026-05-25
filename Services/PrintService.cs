using System.Diagnostics;

namespace OPDClinic.Services;

public class PrintService
{
    /// <summary>Opens the PDF in the system's default viewer (user prints from there).</summary>
    public static void OpenPdf(string pdfPath)
    {
        Process.Start(new ProcessStartInfo(pdfPath) { UseShellExecute = true });
    }

    /// <summary>Sends the PDF silently to the default printer via the shell "print" verb.</summary>
    public static void PrintSilent(string pdfPath)
    {
        var info = new ProcessStartInfo(pdfPath)
        {
            Verb            = "print",
            UseShellExecute = true,
            CreateNoWindow  = true,
            WindowStyle     = ProcessWindowStyle.Hidden,
        };
        Process.Start(info);
    }
}
