using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OPDClinic.Helpers;
using OPDClinic.Models;
using OPDClinic.Services;

namespace OPDClinic.Views;

// ── Data models ───────────────────────────────────────────────────────────────

public class SelectablePermission
{
    public Permission Permission { get; set; }
    public string ActionLabel   { get; set; } = "";
    public string Description   { get; set; } = "";
    public bool   IsSelected    { get; set; }

    /// <summary>Set after UI is built so we can read back the checked state.</summary>
    internal CheckBox? Checkbox { get; set; }
}

public class PermissionSection
{
    public string Name { get; set; } = "";
    public List<SelectablePermission> Items { get; set; } = [];
    internal CheckBox? HeaderCheckBox { get; set; }
}

// ── Dialog ────────────────────────────────────────────────────────────────────

public partial class CustomRoleEditDialog : Window
{
    private readonly CustomRole?           _existing;
    private readonly List<PermissionSection> _sections;
    private bool _isUpdating; // prevents re-entrant checkbox events

    // ── Labels (also used by CustomRolesView detail panel) ───────────────────
    public static readonly Dictionary<Permission, (string Name, string Desc)> Labels = new()
    {
        // Patients
        [Permission.ViewPatients]            = ("View Patients",           "Browse the patient list and open visit records"),
        [Permission.RegisterPatient]         = ("Add Patients / Visits",   "Register a new patient and add new visits"),
        [Permission.EditPatientInfo]         = ("Edit Patient Info",        "Edit patient demographics — name, date of birth, address, phone"),
        [Permission.DeletePatient]           = ("Delete Patient",           "Permanently delete a patient record"),
        [Permission.ExportPatients]          = ("Export Patients",          "Download the patient list as CSV / Excel"),
        [Permission.ViewAllPhysicianPatients]= ("View All Patients",        "See patients from every physician, not just own"),
        // Visits
        [Permission.ViewClinicalData]        = ("View Clinical Data",       "View visit details — vitals, findings and diagnosis — without editing"),
        [Permission.EnterClinicalData]       = ("Edit Clinical Data",       "Record and update vitals, clinical findings and diagnosis"),
        [Permission.DeleteVisit]             = ("Delete Visit",             "Permanently delete a visit record and all its data"),
        [Permission.ExportVisits]            = ("Export Visits",            "Download a patient's visit history as CSV / Excel"),
        // Prescriptions
        [Permission.ViewPrescription]        = ("View Prescription",        "View prescription details — no add or edit"),
        [Permission.AddPrescription]         = ("Add Prescription",         "Add new medicine lines and lab tests to a prescription"),
        [Permission.EditPrescription]        = ("Edit Prescription",        "Update existing prescription medicine lines and lab tests"),
        [Permission.DeletePrescriptionLine]  = ("Delete Prescription Line", "Remove a medicine line or lab test from a prescription"),
        [Permission.PrintPdf]                = ("Print PDF",                "Generate and print the A4 prescription PDF"),
        // Physicians
        [Permission.ViewPhysicians]          = ("View Physicians",          "Browse the physician list — no add or edit"),
        [Permission.AddPhysician]            = ("Add Physician",            "Register a new physician profile"),
        [Permission.EditPhysician]           = ("Edit Physician",           "Update an existing physician's details"),
        [Permission.DeletePhysicians]        = ("Delete Physician",         "Permanently delete a physician record"),
        [Permission.ExportPhysicians]        = ("Export Physicians",        "Download the physician list as CSV / Excel"),
        // Medicine catalog
        [Permission.ViewMedicineCatalog]     = ("View Catalog",             "Browse medicines, categories, routes and dosages — no edit"),
        [Permission.AddMedicine]             = ("Add to Catalog",           "Add new medicines and catalog items (categories, routes, dosages, notes)"),
        [Permission.EditMedicine]            = ("Edit Catalog",             "Update existing medicines and catalog items"),
        [Permission.DeleteMedicineCatalog]   = ("Delete from Catalog",      "Delete medicines, categories, routes, dosages and notes"),
        [Permission.ExportMedicineCatalog]   = ("Export Catalog",           "Download any catalog table as CSV / Excel"),
        // Users
        [Permission.ViewUsers]               = ("View Users",               "Browse the user list — no add or edit"),
        [Permission.AddUser]                 = ("Add User",                 "Create new user accounts"),
        [Permission.EditUser]                = ("Edit User",                "Update accounts, reset passwords and assign roles"),
        [Permission.DeleteUsers]             = ("Delete User",              "Permanently delete a user account"),
        [Permission.ExportUsers]             = ("Export Users",             "Download the user list as CSV / Excel"),
    };

    public CustomRoleEditDialog(CustomRole? existing)
    {
        InitializeComponent();
        DialogHelper.ApplyConstraints(this);
        _existing = existing;

        var existingPerms = existing?.GetPermissions() ?? [];
        _sections = BuildSections(existingPerms);
        BuildPermissionsUI();
        PermissionsScrollView.MaxHeight = DialogHelper.InnerScrollHeight();

        if (existing is not null)
        {
            NameBox.Text        = existing.Name;
            DescriptionBox.Text = existing.Description;
            NotesBox.Text       = existing.AdditionalNotes ?? "";

            if (existing.IsSystem)
            {
                TitleText.Text              = "Edit Built-in Role";
                NameBox.IsEnabled           = false;
                SystemRoleBanner.Visibility = Visibility.Visible;
            }
            else
            {
                TitleText.Text = "Edit Custom Role";
            }
        }
        else
        {
            TitleText.Text = "New Custom Role";
        }
    }

    // ── Section definitions ───────────────────────────────────────────────────

    private static List<PermissionSection> BuildSections(HashSet<Permission> existing) =>
    [
        new PermissionSection
        {
            Name = "Patients",
            Items =
            [
                Perm(Permission.ViewPatients,    "View",   existing),
                Perm(Permission.RegisterPatient, "Add",    existing),
                Perm(Permission.EditPatientInfo, "Edit",   existing),
                Perm(Permission.DeletePatient,   "Delete", existing),
                Perm(Permission.ExportPatients,  "Export", existing),
            ]
        },
        new PermissionSection
        {
            Name = "Visits & Clinical Data",
            Items =
            [
                Perm(Permission.ViewClinicalData,  "View",   existing),
                Perm(Permission.EnterClinicalData, "Edit",   existing),
                Perm(Permission.DeleteVisit,       "Delete", existing),
                Perm(Permission.ExportVisits,      "Export", existing),
            ]
        },
        new PermissionSection
        {
            Name = "Prescriptions",
            Items =
            [
                Perm(Permission.ViewPrescription,       "View",   existing),
                Perm(Permission.AddPrescription,        "Add",    existing),
                Perm(Permission.EditPrescription,       "Edit",   existing),
                Perm(Permission.DeletePrescriptionLine, "Delete", existing),
                Perm(Permission.PrintPdf,               "Print",  existing),
            ]
        },
        new PermissionSection
        {
            Name = "Physicians",
            Items =
            [
                Perm(Permission.ViewPhysicians,   "View",   existing),
                Perm(Permission.AddPhysician,     "Add",    existing),
                Perm(Permission.EditPhysician,    "Edit",   existing),
                Perm(Permission.DeletePhysicians, "Delete", existing),
                Perm(Permission.ExportPhysicians, "Export", existing),
            ]
        },
        new PermissionSection
        {
            Name = "Medicine Catalog",
            Items =
            [
                Perm(Permission.ViewMedicineCatalog,   "View",   existing),
                Perm(Permission.AddMedicine,           "Add",    existing),
                Perm(Permission.EditMedicine,          "Edit",   existing),
                Perm(Permission.DeleteMedicineCatalog, "Delete", existing),
                Perm(Permission.ExportMedicineCatalog, "Export", existing),
            ]
        },
        new PermissionSection
        {
            Name = "User Management",
            Items =
            [
                Perm(Permission.ViewUsers,   "View",   existing),
                Perm(Permission.AddUser,     "Add",    existing),
                Perm(Permission.EditUser,    "Edit",   existing),
                Perm(Permission.DeleteUsers, "Delete", existing),
                Perm(Permission.ExportUsers, "Export", existing),
            ]
        },
        new PermissionSection
        {
            Name = "Other",
            Items =
            [
                Perm(Permission.ViewAllPhysicianPatients, "View All Patients", existing),
            ]
        },
    ];

    private static SelectablePermission Perm(Permission p, string actionLabel, HashSet<Permission> existing)
    {
        Labels.TryGetValue(p, out var label);
        return new SelectablePermission
        {
            Permission  = p,
            ActionLabel = actionLabel,
            Description = label.Desc ?? "",
            IsSelected  = existing.Contains(p),
        };
    }

    // ── Dynamic UI builder ────────────────────────────────────────────────────

    private void BuildPermissionsUI()
    {
        var primaryBrush   = (Brush?)Application.Current.TryFindResource("TextPrimaryBrush")
                             ?? Brushes.Black;
        var secondaryBrush = (Brush?)Application.Current.TryFindResource("TextSecondaryBrush")
                             ?? Brushes.Gray;
        var borderBrush    = (Brush?)Application.Current.TryFindResource("BorderBrush")
                             ?? Brushes.LightGray;
        var headerBrush    = new SolidColorBrush(Color.FromRgb(0x1D, 0x4E, 0xD8));

        bool first = true;
        foreach (var section in _sections)
        {
            if (!first)
            {
                PermissionsContainer.Children.Add(new Border
                {
                    Height     = 1,
                    Background = borderBrush,
                    Margin     = new Thickness(0, 4, 0, 8)
                });
            }
            first = false;

            // ── Section header (select-all checkbox) ─────────────────────────
            var headerCb = new CheckBox
            {
                Content      = section.Name.ToUpper(),
                IsThreeState = true,
                FontWeight   = FontWeights.SemiBold,
                FontSize     = 11,
                Foreground   = headerBrush,
                Margin       = new Thickness(0, 0, 0, 6),
            };
            section.HeaderCheckBox = headerCb;
            UpdateSectionHeader(section); // set initial tri-state

            var capturedSection = section; // capture for closures
            headerCb.Checked   += (_, _) => OnSectionChecked(capturedSection, true);
            headerCb.Unchecked += (_, _) => OnSectionChecked(capturedSection, false);

            PermissionsContainer.Children.Add(headerCb);

            // ── Child permission rows ─────────────────────────────────────────
            foreach (var item in section.Items)
            {
                var row = new Grid { Margin = new Thickness(20, 0, 0, 8) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var childCb = new CheckBox
                {
                    IsChecked       = item.IsSelected,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin          = new Thickness(0, 2, 0, 0),
                };
                item.Checkbox = childCb;

                var capturedItem    = item;
                var capturedSection2 = section;
                childCb.Checked   += (_, _) => { if (!_isUpdating) { capturedItem.IsSelected = true;  UpdateSectionHeader(capturedSection2); } };
                childCb.Unchecked += (_, _) => { if (!_isUpdating) { capturedItem.IsSelected = false; UpdateSectionHeader(capturedSection2); } };

                Grid.SetColumn(childCb, 0);
                row.Children.Add(childCb);

                var labelPanel = new StackPanel { Margin = new Thickness(8, 0, 0, 0) };
                Grid.SetColumn(labelPanel, 1);

                labelPanel.Children.Add(new TextBlock
                {
                    Text       = item.ActionLabel,
                    FontWeight = FontWeights.SemiBold,
                    FontSize   = 12,
                    Foreground = primaryBrush,
                });

                if (!string.IsNullOrEmpty(item.Description))
                {
                    labelPanel.Children.Add(new TextBlock
                    {
                        Text        = item.Description,
                        FontSize    = 11,
                        Foreground  = secondaryBrush,
                        TextWrapping = TextWrapping.Wrap,
                    });
                }

                row.Children.Add(labelPanel);
                PermissionsContainer.Children.Add(row);
            }
        }
    }

    private void OnSectionChecked(PermissionSection section, bool value)
    {
        if (_isUpdating) return;
        _isUpdating = true;
        try
        {
            foreach (var item in section.Items)
            {
                item.IsSelected = value;
                if (item.Checkbox is not null)
                    item.Checkbox.IsChecked = value;
            }
        }
        finally { _isUpdating = false; }
    }

    private void UpdateSectionHeader(PermissionSection section)
    {
        if (section.HeaderCheckBox is null) return;
        _isUpdating = true;
        try
        {
            bool allOn  = section.Items.All(i => i.IsSelected);
            bool allOff = section.Items.All(i => !i.IsSelected);
            section.HeaderCheckBox.IsChecked = allOn ? true : allOff ? false : null;
        }
        finally { _isUpdating = false; }
    }

    // ── Save ──────────────────────────────────────────────────────────────────

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        bool   isSystem   = _existing?.IsSystem == true;
        string description = DescriptionBox.Text.Trim();
        string notes       = NotesBox.Text.Trim();

        var selectedPerms = _sections
            .SelectMany(s => s.Items)
            .Where(p => p.IsSelected)
            .Select(p => p.Permission);

        string name;
        if (isSystem)
        {
            name = _existing!.Name;
        }
        else
        {
            name = NameBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            { ShowError("Role name is required."); return; }
        }

        try
        {
            using var db = App.DbFactory.CreateDbContext();

            if (!isSystem)
            {
                var excludeId = _existing?.Id ?? 0;
                if (db.CustomRoles.Any(r => r.Name.ToLower() == name.ToLower() && r.Id != excludeId))
                { ShowError($"A role named \"{name}\" already exists."); return; }
            }

            if (_existing is not null)
            {
                if (!isSystem) _existing.Name = name;
                _existing.Description     = description;
                _existing.AdditionalNotes = string.IsNullOrEmpty(notes) ? null : notes;
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
                    CreatedAt       = DateTime.UtcNow,
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
