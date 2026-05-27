using System.Reflection;
using System.Windows;

namespace OPDClinic.Views;

public partial class AboutDialog : Window
{
    public AboutDialog()
    {
        InitializeComponent();
        var v = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = v is null ? "" : $"Version {v.Major}.{v.Minor}.{v.Build}";
    }

    private void EmailBtn_Click(object sender, RoutedEventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName        = "mailto:info.rxwriter@gmail.com",
            UseShellExecute = true
        });
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
