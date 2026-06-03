using System.Windows;
using System.Windows.Controls;
using OPDClinic.Models;
using OPDClinic.Services;
using OPDClinic.ViewModels;

namespace OPDClinic.Views;

public partial class UserManagementView : UserControl
{
    public UserManagementViewModel ViewModel { get; } = new();

    public UserManagementView()
    {
        InitializeComponent();
        DataContext = ViewModel;
        ViewModel.LoadUsersCommand.Execute(null);
    }

    private void AddUser_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Auth.Can(Services.Permission.AddUser)) return;
        var dlg = new UserEditDialog(null);
        if (dlg.ShowDialog() != true) return;
        ViewModel.LoadUsersCommand.Execute(null);
    }

    private void EditUser_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Auth.Can(Services.Permission.EditUser)) return;
        if (sender is not Button btn || btn.Tag is not User user) return;
        if (user.Role == UserRole.Admin && !App.Auth.IsFullAdmin)
        {
            MessageBox.Show("Administrator accounts can only be managed by the system Admin.",
                "Access Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var dlg = new UserEditDialog(user);
        if (dlg.ShowDialog() != true) return;
        ViewModel.LoadUsersCommand.Execute(null);
    }

    private void SmtpSettings_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SmtpSettingsDialog { Owner = Window.GetWindow(this) };
        dlg.ShowDialog();
    }
}
