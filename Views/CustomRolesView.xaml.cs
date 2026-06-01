using System.Windows;
using System.Windows.Controls;
using OPDClinic.Models;
using OPDClinic.Services;
using OPDClinic.ViewModels;

namespace OPDClinic.Views;

/// <summary>Lightweight display row used in the detail-panel permissions ItemsControl.</summary>
internal sealed class PermDisplay
{
    public string Granted { get; init; } = "";
    public string Name    { get; init; } = "";
    public string Desc    { get; init; } = "";
}

public partial class CustomRolesView : UserControl
{
    private readonly CustomRolesViewModel _vm;

    public CustomRolesView()
    {
        InitializeComponent();
        _vm = new CustomRolesViewModel();
        DataContext = _vm;

        RolesGrid.SelectionChanged += RolesGrid_SelectionChanged;
        RefreshDetailPanel(null);
    }

    // ── Selection ────────────────────────────────────────────────────────────

    private void RolesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        RefreshDetailPanel(_vm.SelectedRole);

    private void RefreshDetailPanel(CustomRole? role)
    {
        if (role is null)
        {
            NoSelectionPanel.Visibility = Visibility.Visible;
            RoleDetailPanel.Visibility  = Visibility.Collapsed;
            return;
        }

        NoSelectionPanel.Visibility = Visibility.Collapsed;
        RoleDetailPanel.Visibility  = Visibility.Visible;

        // ── Permission rows ──────────────────────────────────────────────────
        var granted = role.GetPermissions();

        DetailPermList.ItemsSource = Enum.GetValues<Permission>()
            .Select(p => new PermDisplay
            {
                Granted = granted.Contains(p) ? "✅" : "❌",
                Name    = CustomRoleEditDialog.Labels.TryGetValue(p, out var l)  ? l.Name : p.ToString(),
                Desc    = CustomRoleEditDialog.Labels.TryGetValue(p, out var l2) ? l2.Desc : ""
            })
            .ToList();

        // ── Backend block ────────────────────────────────────────────────────
        BackendName.Text = role.Name;

        var permNames = granted.Select(p => p.ToString()).ToList();
        BackendPerms.Text = permNames.Count == 0
            ? "[]"
            : $"[ \"{string.Join("\", \"", permNames)}\" ]";
    }

    // ── Button handlers ──────────────────────────────────────────────────────

    private void EditRole_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not CustomRole role) return;

        var dlg = new CustomRoleEditDialog(role) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() != true) return;

        _vm.LoadRolesCommand.Execute(null);
        RefreshDetailPanel(_vm.SelectedRole);
    }

    private void DeleteRole_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not CustomRole role) return;
        _vm.DeleteRoleCommand.Execute(role);
        RefreshDetailPanel(_vm.SelectedRole);
    }
}
