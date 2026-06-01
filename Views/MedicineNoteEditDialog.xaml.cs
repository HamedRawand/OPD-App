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

    /// <summary>All MedicineForm records — used to populate and filter the Type ComboBox.</summary>
    private readonly List<MedicineForm> _allForms;

    public MedicineNoteEditDialog(IDbContextFactory<AppDbContext> factory, MedicineNote? note = null)
    {
        InitializeComponent();
        _factory  = factory;
        _existing = note;

        using var db = factory.CreateDbContext();

        // Populate Category ComboBox from RouteOfAdministration categories
        var categories = db.Routes
            .Select(r => r.Category)
            .Where(c => c != null)
            .Distinct()
            .OrderBy(c => c)
            .ToList();
        CategoryBox.ItemsSource = categories;

        // Load all MedicineForms so we can filter Type by Category
        _allForms = db.MedicineForms
            .OrderBy(f => f.Category)
            .ThenBy(f => f.FormName)
            .ToList();

        // Populate Type ComboBox with all form names initially
        RefreshTypeItems(null);

        if (note is not null)
        {
            HeaderTitle.SetResourceReference(TextBlock.TextProperty, "Options.NoteEdit.Header.Edit");
            NotesBox.Text    = note.Notes    ?? "";
            CategoryBox.Text = note.Category ?? "";
            // Refresh Type list based on the existing category, then restore value
            RefreshTypeItems(note.Category);
            TypeBox.Text = note.Type ?? "";
        }
        else
        {
            HeaderTitle.SetResourceReference(TextBlock.TextProperty, "Options.NoteEdit.Header.Add");
        }
    }

    /// <summary>Refreshes TypeBox items to show only forms whose Category matches <paramref name="category"/>.
    /// Passing null or empty shows all forms.</summary>
    private void RefreshTypeItems(string? category)
    {
        var currentText = TypeBox?.Text ?? "";

        List<string?> formNames;
        if (string.IsNullOrWhiteSpace(category))
        {
            formNames = _allForms
                .Select(f => f.FormName)
                .Distinct()
                .OrderBy(n => n)
                .ToList<string?>();
        }
        else
        {
            formNames = _allForms
                .Where(f => f.Category == category)
                .Select(f => f.FormName)
                .Distinct()
                .OrderBy(n => n)
                .ToList<string?>();
        }

        TypeBox!.ItemsSource = formNames;

        // Restore the typed text so ComboBox.IsEditable doesn't lose the value
        TypeBox.Text = currentText;
    }

    private void CategoryBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = CategoryBox.SelectedItem as string;
        RefreshTypeItems(selected);
        // Clear the type selection when category changes
        TypeBox.Text = "";
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
        item.Type     = string.IsNullOrWhiteSpace(TypeBox.Text)     ? null : TypeBox.Text.Trim();

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
