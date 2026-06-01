using System.Windows;
using System.Windows.Controls;
using OPDClinic.Models;
using OPDClinic.ViewModels;

namespace OPDClinic.Views;

public partial class CatalogView : UserControl
{
    public CatalogViewModel ViewModel { get; }

    public CatalogView()
    {
        InitializeComponent();
        ViewModel = new CatalogViewModel(App.DbFactory);
        DataContext = ViewModel;
    }

    private void AddMedicine_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new MedicineEditDialog(App.DbFactory) { Owner = Window.GetWindow(this) };
        if (dlg.ShowDialog() == true)
            ViewModel.LoadMedicinesCommand.Execute(null);
    }

    private void EditMedicine_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is MedicineList medicine)
        {
            var dlg = new MedicineEditDialog(App.DbFactory, medicine) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true)
                ViewModel.LoadMedicinesCommand.Execute(null);
        }
    }
}
