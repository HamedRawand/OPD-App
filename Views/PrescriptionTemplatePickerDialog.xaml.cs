using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using OPDClinic.Data;
using OPDClinic.Helpers;
using OPDClinic.Models;

namespace OPDClinic.Views;

public partial class PrescriptionTemplatePickerDialog : Window
{
    private readonly List<PrescriptionTemplate> _all;

    /// <summary>Set when the dialog closes with DialogResult == true.</summary>
    public PrescriptionTemplate? SelectedTemplate { get; private set; }

    public PrescriptionTemplatePickerDialog(IDbContextFactory<AppDbContext> factory)
    {
        InitializeComponent();
        DialogHelper.ApplyConstraints(this);

        using var db = factory.CreateDbContext();
        _all = db.PrescriptionTemplates
            .Include(t => t.Lines)
            .Include(t => t.LabTests)
            .OrderBy(t => t.Name)
            .ToList();

        ResultsList.ItemsSource = _all;
    }

    private void SearchBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        var q = SearchBox.Text.Trim();
        ResultsList.ItemsSource = string.IsNullOrEmpty(q)
            ? _all
            : _all.Where(t =>
                    t.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (t.Description?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
    }

    private void ResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e) => TrySelect();

    private void Select_Click(object sender, RoutedEventArgs e) => TrySelect();

    private void TrySelect()
    {
        if (ResultsList.SelectedItem is PrescriptionTemplate template)
        {
            SelectedTemplate = template;
            DialogResult = true;
            Close();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
