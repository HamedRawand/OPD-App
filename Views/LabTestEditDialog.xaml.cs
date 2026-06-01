using System.Windows;
using Microsoft.EntityFrameworkCore;
using OPDClinic.Data;
using OPDClinic.Helpers;
using OPDClinic.Models;

namespace OPDClinic.Views;

public partial class LabTestEditDialog : Window
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly LabTest? _existing;

    public LabTestEditDialog(IDbContextFactory<AppDbContext> factory, LabTest? labTest = null)
    {
        InitializeComponent();
        _factory  = factory;
        _existing = labTest;

        if (labTest is not null)
        {
            HeaderTitle.Text     = "Edit Lab Test";
            CategoryBox.Text     = labTest.Category     ?? "";
            TestNameBox.Text     = labTest.TestName     ?? "";
            AbbreviationBox.Text = labTest.Abbreviation ?? "";
            SpecimenBox.Text     = labTest.Specimen     ?? "";
            DescriptionBox.Text  = labTest.Description  ?? "";
        }
        else
        {
            HeaderTitle.Text = "Add Lab Test";
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = TestNameBox.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ShowError("Test name is required.");
            return;
        }

        var item = _existing ?? new LabTest();
        item.Category     = CategoryBox.Text.NullIfEmpty();
        item.TestName     = name;
        item.Abbreviation = AbbreviationBox.Text.NullIfEmpty();
        item.Specimen     = SpecimenBox.Text.NullIfEmpty();
        item.Description  = DescriptionBox.Text.NullIfEmpty();

        try
        {
            using var db = _factory.CreateDbContext();
            if (_existing is null)
                db.LabTests.Add(item);
            else
                db.Update(item);
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            ShowError($"Could not save lab test:\n{ex.Message}");
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
