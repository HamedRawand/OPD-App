using System.Windows;
using System.Windows.Controls;
using OPDClinic.Data;
using OPDClinic.Models;

namespace OPDClinic.Views;

public partial class MedicineNoteEditDialog : Window
{
    private readonly AppDbContext _db;
    private readonly MedicineNote? _existing;

    public MedicineNoteEditDialog(AppDbContext db, MedicineNote? note = null)
    {
        InitializeComponent();
        _db = db;
        _existing = note;

        // Populate category ComboBox from RouteOfAdministration categories
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
            NotesBox.Text = note.Notes ?? "";
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
        item.Notes = notes;
        item.Category = string.IsNullOrWhiteSpace(CategoryBox.Text) ? null : CategoryBox.Text.Trim();

        if (_existing is null)
            _db.MedicineNotes.Add(item);

        try { _db.SaveChanges(); }
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
