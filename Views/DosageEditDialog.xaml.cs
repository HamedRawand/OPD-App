using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using OPDClinic.Data;
using OPDClinic.Helpers;
using OPDClinic.Models;

namespace OPDClinic.Views;

public partial class DosageEditDialog : Window
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly Dosage? _existing;
    private readonly List<MedicineForm> _allForms;
    private List<TypeCheckItem> _typeItems = new();

    public DosageEditDialog(IDbContextFactory<AppDbContext> factory, Dosage? dosage = null)
    {
        InitializeComponent();
        DialogHelper.ApplyConstraints(this);
        _factory  = factory;
        _existing = dosage;

        using var db = factory.CreateDbContext();

        var categories = db.Routes
            .Select(r => r.Category)
            .Where(c => c != null)
            .Distinct()
            .OrderBy(c => c)
            .ToList();
        CategoryBox.ItemsSource = categories;

        _allForms = db.MedicineForms
            .OrderBy(f => f.Category)
            .ThenBy(f => f.FormName)
            .ToList();

        RefreshTypeItems(null);

        if (dosage is not null)
        {
            HeaderTitle.SetResourceReference(TextBlock.TextProperty, "Options.DosageEdit.Header.Edit");
            CategoryBox.Text   = dosage.Category  ?? "";
            RefreshTypeItems(dosage.Category);
            ApplyExistingTypes(dosage.Type);
            DosageTextBox.Text = dosage.DosageText ?? "";
        }
        else
        {
            HeaderTitle.SetResourceReference(TextBlock.TextProperty, "Options.DosageEdit.Header.Add");
        }
    }

    private void RefreshTypeItems(string? category)
    {
        var currentlyChecked = _typeItems.Where(i => i.IsChecked).Select(i => i.Name).ToHashSet();

        IEnumerable<string> names = string.IsNullOrWhiteSpace(category)
            ? _allForms.Select(f => f.FormName).Where(n => n != null).Distinct().OrderBy(n => n)!
            : _allForms.Where(f => f.Category == category).Select(f => f.FormName).Where(n => n != null).Distinct().OrderBy(n => n)!;

        _typeItems = names.Select(n => new TypeCheckItem { Name = n, IsChecked = currentlyChecked.Contains(n) }).ToList();
        TypeItemsControl.ItemsSource = _typeItems;
        UpdateTypeToggleText();
    }

    private void ApplyExistingTypes(string? typeString)
    {
        if (string.IsNullOrWhiteSpace(typeString)) return;
        var parts = typeString.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();
        foreach (var item in _typeItems)
            item.IsChecked = parts.Contains(item.Name);
        UpdateTypeToggleText();
    }

    private void UpdateTypeToggleText()
    {
        var selected = _typeItems.Where(i => i.IsChecked).Select(i => i.Name).ToList();
        TypeToggleText.Text = selected.Count > 0 ? string.Join(", ", selected) : "";
    }

    private void TypeCheckBox_Changed(object sender, RoutedEventArgs e) => UpdateTypeToggleText();

    private void CategoryBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = CategoryBox.SelectedItem as string;
        RefreshTypeItems(selected);
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var text = DosageTextBox.Text.Trim();
        if (string.IsNullOrEmpty(text))
        {
            ShowError("Dosage text is required.");
            return;
        }

        var selectedTypes = string.Join(",", _typeItems.Where(i => i.IsChecked).Select(i => i.Name));

        var item = _existing ?? new Dosage();
        item.Category   = CategoryBox.Text.NullIfEmpty();
        item.Type       = string.IsNullOrEmpty(selectedTypes) ? null : selectedTypes;
        item.DosageText = text;

        try
        {
            using var db = _factory.CreateDbContext();
            if (_existing is null)
                db.Dosages.Add(item);
            else
                db.Update(item);
            db.SaveChanges();
        }
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

    private sealed class TypeCheckItem : INotifyPropertyChanged
    {
        public string Name { get; init; } = "";
        private bool _isChecked;
        public bool IsChecked
        {
            get => _isChecked;
            set { _isChecked = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsChecked))); }
        }
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
