using System.Windows;
using System.Windows.Controls;
using OPDClinic.Data;
using OPDClinic.Helpers;
using OPDClinic.Models;

namespace OPDClinic.Views;

public partial class MedicineCategoryEditDialog : Window
{
    private readonly AppDbContext _db;
    private readonly MedicineForm? _existing;

    public MedicineCategoryEditDialog(AppDbContext db, MedicineForm? form = null)
    {
        InitializeComponent();
        _db = db;
        _existing = form;

        // Populate category dropdown from distinct Route categories
        var categories = db.Routes
            .Select(r => r.Category)
            .Where(c => c != null)
            .Distinct()
            .OrderBy(c => c)
            .ToList();
        CategoryBox.ItemsSource = categories;

        if (form is not null)
        {
            HeaderTitle.SetResourceReference(TextBlock.TextProperty, "Options.MedCatEdit.Header.Edit");
            CategoryBox.Text     = form.Category     ?? "";
            FormNameBox.Text     = form.FormName     ?? "";
            AbbreviationBox.Text = form.Abbreviation ?? "";
            NoteBox.Text         = form.Note         ?? "";
        }
        else
        {
            HeaderTitle.SetResourceReference(TextBlock.TextProperty, "Options.MedCatEdit.Header.Add");
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var formName = FormNameBox.Text.Trim();
        if (string.IsNullOrEmpty(formName))
        {
            ShowError("Form name is required.");
            return;
        }

        var item = _existing ?? new MedicineForm();
        item.Category     = CategoryBox.Text.NullIfEmpty();
        item.FormName     = formName;
        item.Abbreviation = AbbreviationBox.Text.NullIfEmpty();
        item.Note         = NoteBox.Text.NullIfEmpty();

        if (_existing is null)
            _db.MedicineForms.Add(item);

        try { _db.SaveChanges(); }
        catch (Exception ex)
        {
            ShowError($"Could not save medicine form:\n{ex.Message}");
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
