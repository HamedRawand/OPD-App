using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using OPDClinic.Data;
using OPDClinic.Helpers;
using OPDClinic.Models;
using OPDClinic.Services;
using OPDClinic.ViewModels;

namespace OPDClinic.Views;

public partial class PrescriptionTemplateEditDialog : Window
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly PrescriptionTemplate? _existing;
    private readonly PrescriptionViewModel _rxVm;

    public PrescriptionTemplateEditDialog(IDbContextFactory<AppDbContext> factory, PrescriptionTemplate? template = null)
    {
        InitializeComponent();
        DialogHelper.ApplyConstraints(this);
        _factory  = factory;
        _existing = template;

        _rxVm = new PrescriptionViewModel(factory);
        RxView.DataContext = _rxVm;

        if (template is not null)
        {
            HeaderTitle.SetResourceReference(TextBlock.TextProperty, "RxTemplate.Header.Edit");
            NameBox.Text              = template.Name;
            DescriptionBox.Text       = template.Description        ?? "";
            DefaultDiagnosisBox.Text  = template.DefaultDiagnosis    ?? "";
            DefaultFindingsBox.Text   = template.DefaultClinicalFindings ?? "";

            // Reload full template (with children) — the row passed in from the
            // Options grid may not have Lines/LabTests included.
            using var db = factory.CreateDbContext();
            var full = db.PrescriptionTemplates
                .Include(t => t.Lines)
                .Include(t => t.LabTests)
                .FirstOrDefault(t => t.Id == template.Id);

            if (full is not null)
            {
                foreach (var line in full.Lines.OrderBy(l => l.SortOrder))
                {
                    _rxVm.Lines.Add(new MedicineUsage
                    {
                        LineNumber   = _rxVm.Lines.Count + 1,
                        Prescription = line.Prescription,
                        Type         = line.Type,
                        Strength     = line.Strength,
                        Qty          = line.Qty,
                        Usage        = line.Usage,
                        Note         = line.Note,
                    });
                }

                var labTestIds = full.LabTests.Select(lt => lt.LabTestId).ToHashSet();
                foreach (var group in _rxVm.LabTestGroups)
                    foreach (var test in group.Tests)
                        if (labTestIds.Contains(test.Test.Id))
                            test.IsSelected = true;

                if (!string.IsNullOrEmpty(template.FooterNote))
                    _rxVm.SelectedPrescriptionNote =
                        _rxVm.PrescriptionNotes.FirstOrDefault(n => n.Notes == template.FooterNote);
            }
        }
        else
        {
            HeaderTitle.SetResourceReference(TextBlock.TextProperty, "RxTemplate.Header.Add");
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = NameBox.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ShowError("Template name is required.");
            return;
        }

        if (_rxVm.Lines.Count == 0 && _rxVm.SelectedLabTests.Count == 0)
        {
            ShowError("Add at least one medicine line or lab test to the template.");
            return;
        }

        bool isNew = _existing is null;
        try
        {
            using var db = _factory.CreateDbContext();

            if (!isNew)
            {
                var excludeId = _existing!.Id;
                if (db.PrescriptionTemplates.Any(t => t.Name.ToLower() == name.ToLower() && t.Id != excludeId))
                { ShowError($"A template named \"{name}\" already exists."); return; }
            }
            else if (db.PrescriptionTemplates.Any(t => t.Name.ToLower() == name.ToLower()))
            {
                ShowError($"A template named \"{name}\" already exists.");
                return;
            }

            PrescriptionTemplate template;
            if (isNew)
            {
                template = new PrescriptionTemplate { CreatedAt = DateTime.UtcNow };
                db.PrescriptionTemplates.Add(template);
            }
            else
            {
                template = db.PrescriptionTemplates.Find(_existing!.Id)
                           ?? throw new InvalidOperationException(
                                  $"Template record (Id={_existing.Id}) was not found.");
            }

            template.Name                    = name;
            template.Description             = DescriptionBox.Text.Trim().NullIfEmpty();
            template.DefaultDiagnosis        = DefaultDiagnosisBox.Text.Trim().NullIfEmpty();
            template.DefaultClinicalFindings = DefaultFindingsBox.Text.Trim().NullIfEmpty();
            template.FooterNote              = _rxVm.SelectedPrescriptionNote?.Notes;
            db.SaveChanges(); // ensure Id is available for new templates

            // Replace child rows
            var existingLines = db.PrescriptionTemplateLines
                .Where(l => l.PrescriptionTemplateId == template.Id).ToList();
            db.PrescriptionTemplateLines.RemoveRange(existingLines);

            var existingLabs = db.PrescriptionTemplateLabTests
                .Where(l => l.PrescriptionTemplateId == template.Id).ToList();
            db.PrescriptionTemplateLabTests.RemoveRange(existingLabs);
            db.SaveChanges();

            for (int i = 0; i < _rxVm.Lines.Count; i++)
            {
                var line = _rxVm.Lines[i];
                db.PrescriptionTemplateLines.Add(new PrescriptionTemplateLine
                {
                    PrescriptionTemplateId = template.Id,
                    SortOrder              = i + 1,
                    Prescription           = line.Prescription,
                    Type                   = line.Type,
                    Strength               = line.Strength,
                    Qty                    = line.Qty,
                    Usage                  = line.Usage,
                    Note                   = line.Note,
                });
            }

            foreach (var test in _rxVm.SelectedLabTests)
                db.PrescriptionTemplateLabTests.Add(new PrescriptionTemplateLabTest
                {
                    PrescriptionTemplateId = template.Id,
                    LabTestId              = test.Test.Id,
                });

            db.SaveChanges();
            AuditService.Log(
                isNew ? "PrescriptionTemplateCreated" : "PrescriptionTemplateUpdated",
                "PrescriptionTemplate", template.Id, template.Name);
        }
        catch (Exception ex)
        {
            ShowError($"Could not save template:\n{ex.Message}");
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
