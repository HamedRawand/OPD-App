using System.Windows;
using System.Windows.Controls;
using OPDClinic.Data;
using OPDClinic.Models;

namespace OPDClinic.Views;

public partial class PrescriptionLineEditDialog : Window
{
    private readonly MedicineUsage      _line;
    private readonly List<MedicineList> _allMedicines;
    private readonly List<Dosage>       _allDosages;
    private readonly List<MedicineNote> _allNotes;

    public PrescriptionLineEditDialog(AppDbContext db, MedicineUsage line)
    {
        InitializeComponent();
        _line = line;

        // Load catalog data fresh from DB
        _allMedicines = db.MedicineLists.OrderBy(m => m.MedicineName).ToList();
        _allDosages   = db.Dosages.ToList();
        _allNotes     = db.MedicineNotes.ToList();
        var forms     = db.MedicineForms.OrderBy(f => f.FormName).ToList();

        FormBox.ItemsSource = forms;

        // Pre-fill text fields
        StrengthBox.Text = line.Strength ?? "";
        QtyBox.Text      = line.Qty?.ToString() ?? "";

        // Pre-select Form — triggers FormBox_SelectionChanged which filters other lists
        var matchedForm = forms.FirstOrDefault(f => f.FormName == line.Type);
        FormBox.SelectedItem = matchedForm;

        // If no form matched, seed unfiltered lists
        if (matchedForm is null)
        {
            MedicineBox.ItemsSource = _allMedicines;
            DosageBox.ItemsSource   = _allDosages;
            NoteBox.ItemsSource     = _allNotes;
        }

        // Pre-fill medicine name (free-text — may not exist in catalog)
        MedicineBox.Text = line.Prescription ?? "";

        // Pre-select Dosage + Note by stored text value
        if (!string.IsNullOrEmpty(line.Usage))
            DosageBox.SelectedItem = (DosageBox.ItemsSource as IEnumerable<Dosage>)?
                .FirstOrDefault(d => d.DosageText == line.Usage);

        if (!string.IsNullOrEmpty(line.Note))
            NoteBox.SelectedItem = (NoteBox.ItemsSource as IEnumerable<MedicineNote>)?
                .FirstOrDefault(n => n.Notes == line.Note);
    }

    // ── Form changed → cascade-filter Medicine, Dosage, Note ─────────────────
    private void FormBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FormBox.SelectedItem is not MedicineForm form)
        {
            MedicineBox.ItemsSource = _allMedicines;
            DosageBox.ItemsSource   = _allDosages;
            NoteBox.ItemsSource     = _allNotes;
            return;
        }

        MedicineBox.ItemsSource = _allMedicines
            .Where(m => m.Type == form.FormName)
            .ToList();

        DosageBox.ItemsSource = _allDosages
            .Where(d => d.Category == form.Category || string.IsNullOrEmpty(d.Category))
            .ToList();

        NoteBox.ItemsSource = _allNotes
            .Where(n => n.Category == form.Category || string.IsNullOrEmpty(n.Category))
            .ToList();
    }

    // ── Save ─────────────────────────────────────────────────────────────────
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var medicineName = MedicineBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(medicineName))
        {
            ShowError("Medicine name is required.");
            return;
        }

        _line.Prescription = medicineName;
        _line.Type         = (FormBox.SelectedItem as MedicineForm)?.FormName;
        _line.Strength     = StrengthBox.Text.Trim().NullIfEmpty();
        _line.Qty          = int.TryParse(QtyBox.Text, out var q) ? q : null;
        _line.Usage        = (DosageBox.SelectedItem as Dosage)?.DosageText;
        _line.Note         = (NoteBox.SelectedItem as MedicineNote)?.Notes;
        // RouteName intentionally not updated — field kept in model for imported data only

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ShowError(string msg)
    {
        ErrorText.Text         = msg;
        ErrorBorder.Visibility = Visibility.Visible;
    }
}

// Local NullIfEmpty helper
file static class Ext
{
    public static string? NullIfEmpty(this string? s) =>
        string.IsNullOrWhiteSpace(s) ? null : s;
}
