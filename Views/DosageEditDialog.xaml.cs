using System.Windows;
using System.Windows.Controls;
using OPDClinic.Data;
using OPDClinic.Helpers;
using OPDClinic.Models;

namespace OPDClinic.Views;

public partial class DosageEditDialog : Window
{
    private readonly AppDbContext _db;
    private readonly Dosage? _existing;

    public DosageEditDialog(AppDbContext db, Dosage? dosage = null)
    {
        InitializeComponent();
        _db = db;
        _existing = dosage;

        // Populate category dropdown from distinct Route categories
        var categories = db.Routes
            .Select(r => r.Category)
            .Where(c => c != null)
            .Distinct()
            .OrderBy(c => c)
            .ToList();
        CategoryBox.ItemsSource = categories;

        if (dosage is not null)
        {
            HeaderTitle.SetResourceReference(TextBlock.TextProperty, "Options.DosageEdit.Header.Edit");
            CategoryBox.Text   = dosage.Category   ?? "";
            TypeBox.Text       = dosage.Type        ?? "";
            DosageTextBox.Text = dosage.DosageText  ?? "";
        }
        else
        {
            HeaderTitle.SetResourceReference(TextBlock.TextProperty, "Options.DosageEdit.Header.Add");
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var text = DosageTextBox.Text.Trim();
        if (string.IsNullOrEmpty(text))
        {
            ShowError("Dosage text is required.");
            return;
        }

        var item = _existing ?? new Dosage();
        item.Category   = CategoryBox.Text.NullIfEmpty();
        item.Type       = TypeBox.Text.NullIfEmpty();
        item.DosageText = text;

        if (_existing is null)
            _db.Dosages.Add(item);

        try { _db.SaveChanges(); }
        catch (Exception ex)
        {
            ShowError($"Could not save dosage:\n{ex.Message}");
            return;
        }
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorBorder.Visibility = Visibility.Visible;
    }
}
