using System.Windows;
using OPDClinic.Models;
using OPDClinic.Services;

namespace OPDClinic.Views;

/// <summary>Selectable wrapper for a Permission enum value shown in the dialog checkbox list.</summary>
public class SelectablePermission
{
    public Permission Permission { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsSelected { get; set; }
}

public partial class CustomRoleEditDialog : Window
{
    private readonly CustomRole? _existing;
    private readonly List<SelectablePermission> _permItems;

    /// <summary>Friendly display info for each permission.</summary>
    public static readonly Dictionary<Permission, (string Name, string Desc)> Labels = new()
    {
        [Permission.ViewPatients]             = ("View Patients",            "Browse the patient list and open visit details"),
        [Permission.RegisterPatient]          = ("Register Patient",         "Create new patient records and add new visits"),
        [Permission.EnterClinicalData]        = ("Enter Clinical Data",      "Edit vitals, clinical findings and diagnosis for existing visits; also allows deleting visits"),
        [Permission.WritePrescription]        = ("Write Prescriptions",      "Add, edit and delete prescription medicine lines and lab tests"),
        [Permission.PrintPdf]                 = ("Print / Export PDF",       "Generate and print the A4 prescription PDF"),
        [Permission.ManagePhysicians]         = ("Manage Physicians",        "Add, edit and delete physician profiles"),
        [Permission.ManageMedicineCatalog]    = ("Manage Medicine Catalog",  "Add, edit and delete medicines in the catalog"),
        [Permission.ManageUsers]              = ("Manage Users",             "Create, edit, deactivate and delete user accounts"),
        [Permission.ViewAllPhysicianPatients] = ("View All Patients",        "See patients from every physician, not just own"),
    };

    public CustomRoleEditDialog(CustomRole? existing)
    {
        InitializeComponent();
        _existing = existing;

        var existingPerms = existing?.GetPermissions() ?? [];

        _permItems = Enum.GetValues<Permission>()
            .Select(p => new SelectablePermission
            {
                Permission  = p,
                Name        = Labels.TryGetValue(p, out var l) ? l.Name : p.ToString(),
                Description = Labels.TryGetValue(p, out var l2) ? l2.Desc : "",
                IsSelected  = existingPerms.Contains(p)
            })
            .ToList();

        PermissionsList.ItemsSource = _permItems;

        if (existing is not null)
        {
            TitleText.Text       = "Edit Custom Role";
            NameBox.Text         = existing.Name;
            DescriptionBox.Text  = existing.Description;
            NotesBox.Text        = existing.AdditionalNotes ?? "";
        }
        else
        {
            TitleText.Text = "New Custom Role";
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        var name        = NameBox.Text.Trim();
        var description = DescriptionBox.Text.Trim();
        var notes       = NotesBox.Text.Trim();

        if (string.IsNullOrEmpty(name))
        { ShowError("Role name is required."); return; }

        var selectedPerms = _permItems.Where(p => p.IsSelected).Select(p => p.Permission);

        try
        {
            using var db = App.DbFactory.CreateDbContext();

            // Uniqueness check
            var excludeId = _existing?.Id ?? 0;
            if (db.CustomRoles.Any(r => r.Name.ToLower() == name.ToLower() && r.Id != excludeId))
            { ShowError($"A role named \"{name}\" already exists."); return; }

            if (_existing is not null)
            {
                _existing.Name             = name;
                _existing.Description      = description;
                _existing.AdditionalNotes  = string.IsNullOrEmpty(notes) ? null : notes;
                _existing.SetPermissions(selectedPerms);
                db.Update(_existing);
            }
            else
            {
                var role = new CustomRole
                {
                    Name            = name,
                    Description     = description,
                    AdditionalNotes = string.IsNullOrEmpty(notes) ? null : notes,
                    CreatedAt       = DateTime.UtcNow
                };
                role.SetPermissions(selectedPerms);
                db.CustomRoles.Add(role);
            }

            db.SaveChanges();
            AuditService.Log(
                _existing is not null ? "CustomRoleUpdated" : "CustomRoleCreated",
                "CustomRole", _existing?.Id, name);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            ShowError($"Could not save role:\n{ex.Message}");
        }
    }

    private void ShowError(string msg)
    {
        ErrorText.Text       = msg;
        ErrorText.Visibility = Visibility.Visible;
    }
}
