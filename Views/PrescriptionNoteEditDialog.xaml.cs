using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using OPDClinic.Data;
using OPDClinic.Helpers;
using OPDClinic.Models;

namespace OPDClinic.Views;

public partial class PrescriptionNoteEditDialog : Window
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly PrescriptionNote? _existing;

    public PrescriptionNoteEditDialog(IDbContextFactory<AppDbContext> factory, PrescriptionNote? note = null)
    {
        InitializeComponent();
        DialogHelper.ApplyConstraints(this);
        _factory  = factory;
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

        try
        {
            using var db = _factory.CreateDbContext();
            if (_existing is null)
                db.PrescriptionNotes.Add(item);
            else
                db.Update(item);
            db.SaveChanges();
        }
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
