using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using OPDClinic.Data;
using OPDClinic.Models;

namespace OPDClinic.Views;

public partial class MedicineNoteEditDialog : Window
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly MedicineNote? _existing;

    public MedicineNoteEditDialog(IDbContextFactory<AppDbContext> factory, MedicineNote? note = null)
    {
        InitializeComponent();
        _factory  = factory;
        _existing = note;

        // Populate category ComboBox from RouteOfAdministration categories
        using var db = factory.CreateDbContext();
        var categories = db.Routes
            .Select(r => r.Category)
            .Where(c => c != null)
            .Distinct()
            .OrderBy(c => c)
            .ToList();
        CategoryBox.ItemsSource = categories;

        if (note is not null)
        {
            HeaderTitle.SetResourceReference(TextBlock.TextProperty, "Options.NoteEdit.Header.Edit");
            NotesBox.Text    = note.Notes    ?? "";
            CategoryBox.Text = note.Category ?? "";
        }
        else
        {
            HeaderTitle.SetResourceReference(TextBlock.TextProperty, "Options.NoteEdit.Header.Add");
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var notes = NotesBox.Text.Trim();
        if (string.IsNullOrEmpty(notes))
        {
            ShowError("Note text is required.");
            return;
        }

        var item = _existing ?? new MedicineNote();
        item.Notes    = notes;
        item.Category = string.IsNullOrWhiteSpace(CategoryBox.Text) ? null : CategoryBox.Text.Trim();

        try
        {
            using var db = _factory.CreateDbContext();
            if (_existing is null)
                db.MedicineNotes.Add(item);
            else
                db.Update(item);
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            ShowError($"Could not save medicine note:\n{ex.Message}");
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
