using System.Windows;
using System.Windows.Controls;
using OPDClinic.Models;
using OPDClinic.Services;
using OPDClinic.ViewModels;
using Application = System.Windows.Application;

namespace OPDClinic.Views;

public partial class PrescriptionView : UserControl
{
    public PrescriptionView()
    {
        InitializeComponent();
    }

    private void DeleteLine_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Auth.CanAny(Permission.WritePrescription, Permission.DeletePrescriptionLine)) return;
        if (sender is Button btn && btn.Tag is MedicineUsage line &&
            DataContext is PrescriptionViewModel vm)
        {
            vm.RemoveLineCommand.Execute(line);
        }
    }

    private void DeselectTest_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is SelectableLabTest test)
            test.IsSelected = false;
    }

    private void EditLine_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Auth.CanAny(Permission.WritePrescription, Permission.EditPrescription)) return;
        if (sender is not Button btn || btn.Tag is not MedicineUsage line) return;
        if (DataContext is not PrescriptionViewModel vm) return;

        var dlg = new PrescriptionLineEditDialog(App.DbFactory, line)
        {
            Owner = Window.GetWindow(this) ?? App.Current.MainWindow
        };

        if (dlg.ShowDialog() != true) return;

        // Force the DataGrid to refresh this row (MedicineUsage has no INPC)
        var idx = vm.Lines.IndexOf(line);
        if (idx >= 0)
        {
            vm.Lines.RemoveAt(idx);
            vm.Lines.Insert(idx, line);
        }
    }
}
