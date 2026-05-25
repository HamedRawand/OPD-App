using System.Windows;
using System.Windows.Controls;
using OPDClinic.Models;
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
        var dlg = new UserEditDialog(null);
        if (dlg.ShowDialog() != true) return;
        ViewModel.LoadUsersCommand.Execute(null);
    }

    private void EditUser_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not User user) return;
        var dlg = new UserEditDialog(user);
        if (dlg.ShowDialog() != true) return;
        ViewModel.LoadUsersCommand.Execute(null);
    }
}
