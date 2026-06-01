using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using OPDClinic.Data;
using OPDClinic.Helpers;
using OPDClinic.Models;

namespace OPDClinic.Views;

public partial class MedicineCategoryEditDialog : Window
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly MedicineForm? _existing;

    public MedicineCategoryEditDialog(IDbContextFactory<AppDbContext> factory, MedicineForm? form = null)
    {
        InitializeComponent();
        _factory  = factory;
        _existing = form;

        // Populate category dropdown from distinct Route categories
        using var db = factory.CreateDbContext();
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

        try
        {
            using var db = _factory.CreateDbContext();
            if (_existing is null)
                db.MedicineForms.Add(item);
            else
                db.Update(item);
            db.SaveChanges();
        }
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
