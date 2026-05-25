using System.Windows;
using System.Windows.Controls;
using OPDClinic.Data;
using OPDClinic.Models;

namespace OPDClinic.Views;

public partial class PrescriptionNoteEditDialog : Window
{
    private readonly AppDbContext _db;
    private readonly PrescriptionNote? _existing;

    public PrescriptionNoteEditDialog(AppDbContext db, PrescriptionNote? note = null)
    {
        InitializeComponent();
        _db = db;
        _existing = note;

        if (note is not null)
        {
            HeaderTitle.SetResourceReference(TextBlock.TextProperty, "Options.RxNoteEdit.Header.Edit");
            NotesBox.Text = note.Notes ?? "";
        }
        else
        {
            HeaderTitle.SetResourceReference(TextBlock.TextProperty, "Options.RxNoteEdit.Header.Add");
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

        var item = _existing ?? new PrescriptionNote();
        item.Notes = notes;

        if (_existing is null)
            _db.PrescriptionNotes.Add(item);

        try { _db.SaveChanges(); }
        catch (Exception ex)
        {
            ShowError($"Could not save prescription note:\n{ex.Message}");
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
