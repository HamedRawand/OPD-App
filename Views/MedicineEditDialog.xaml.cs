using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using OPDClinic.Data;
using OPDClinic.Models;
using OPDClinic.Services;

namespace OPDClinic.Views;

public partial class MedicineEditDialog : Window
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly MedicineList? _existing;
    private List<MedicineForm> _allForms = [];

    public MedicineEditDialog(IDbContextFactory<AppDbContext> factory, MedicineList? medicine = null)
    {
        InitializeComponent();
        _factory  = factory;
        _existing = medicine;

        using var db = factory.CreateDbContext();

        var categories = db.MedicineForms
            .Select(f => f.Category)
            .Where(c => c != null)
            .Distinct()
            .OrderBy(c => c)
            .ToList();
        CategoryBox.ItemsSource = categories;

        _allForms = db.MedicineForms.OrderBy(f => f.FormName).ToList();
        TypeBox.ItemsSource = _allForms;

        if (medicine is not null)
        {
            HeaderTitle.SetResourceReference(TextBlock.TextProperty, "MedEdit.Header.Edit");
            MedicineNameBox.Text = medicine.MedicineName ?? "";
            GenericNameBox.Text  = medicine.GenericName  ?? "";
            StrengthBox.Text     = medicine.Strength     ?? "";
            NoteBox.Text         = medicine.Note         ?? "";

            CategoryBox.SelectedItem = categories.FirstOrDefault(c => c == medicine.Category);
            TypeBox.SelectedItem     = _allForms.FirstOrDefault(f => f.FormName == medicine.Type);
        }
        else
        {
            HeaderTitle.SetResourceReference(TextBlock.TextProperty, "MedEdit.Header.Add");
            if (categories.Count > 0)
            {
                CategoryBox.SelectedIndex = 0;
                FilterFormsByCategory(categories[0]!);
            }
        }
    }

    private void CategoryBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CategoryBox.SelectedItem is string cat)
            FilterFormsByCategory(cat);
    }

    private void FilterFormsByCategory(string category)
    {
        var filtered = string.IsNullOrEmpty(category)
            ? _allForms
            : _allForms.Where(f => f.Category == category || f.Category == null).ToList();
        TypeBox.ItemsSource = filtered;
        TypeBox.SelectedIndex = filtered.Count > 0 ? 0 : -1;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = MedicineNameBox.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ShowError("Medicine name is required.");
            return;
        }

        var medicine = _existing ?? new MedicineList();
        medicine.MedicineName = name;
        medicine.GenericName  = GenericNameBox.Text.Trim();
        medicine.Category     = CategoryBox.SelectedItem as string;
        medicine.Type         = (TypeBox.SelectedItem as MedicineForm)?.FormName;
        medicine.Strength     = StrengthBox.Text.Trim();
        medicine.Note         = NoteBox.Text.Trim();

        bool isNew = _existing is null;
        try
        {
            using var db = _factory.CreateDbContext();
            if (isNew)
                db.MedicineLists.Add(medicine);
            else
                db.Update(medicine);
            db.SaveChanges();
            AuditService.Log(
                isNew ? "MedicineCreated" : "MedicineUpdated",
                "Medicine", medicine.Id, medicine.MedicineName);
        }
        catch (Exception ex)
        {
            ShowError($"Could not save medicine:\n{ex.Message}");
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
