using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using OPDClinic.Data;
using OPDClinic.Models;
using OPDClinic.Services;

namespace OPDClinic.Views;

public partial class PhysicianEditDialog : Window
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly Physician? _existing;
    private byte[]? _symbolBytes;

    public PhysicianEditDialog(IDbContextFactory<AppDbContext> factory, Physician? physician = null)
    {
        InitializeComponent();
        _factory  = factory;
        _existing = physician;

        HeaderTitle.SetResourceReference(TextBlock.TextProperty, "PhysEdit.Header.Add");

        if (physician is not null)
        {
            HeaderTitle.SetResourceReference(TextBlock.TextProperty, "PhysEdit.Header.Edit");
            NameEngBox.Text             = physician.NameEng ?? "";
            SpecialityEngBox.Text       = physician.SpecialityEng ?? "";
            OtherSpecialityEngBox.Text  = physician.OtherSpecialityEng ?? "";
            NameDariBox.Text            = physician.NameDari ?? "";
            SpecialityDariBox.Text      = physician.SpecialityDari ?? "";
            OtherSpecialityDariBox.Text = physician.OtherSpecialityDari ?? "";
            ContactBox.Text             = physician.ContactNumber ?? "";
            WhatsAppBox.Text            = physician.WhatsAppNumber ?? "";
            ReceptionBox.Text           = physician.ReceptionContactNumber ?? "";
            AddressBox.Text             = physician.Address ?? "";

            if (physician.SymbolImage is { Length: > 0 })
            {
                _symbolBytes = physician.SymbolImage;
                ShowSymbolPreview(_symbolBytes);
            }
        }
    }

    // ── Symbol image ──────────────────────────────────────────────────────────

    private void BrowseSymbol_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select physician symbol / logo",
            Filter = "Image files (*.png;*.jpg;*.jpeg;*.bmp;*.gif)|*.png;*.jpg;*.jpeg;*.bmp;*.gif|All files (*.*)|*.*",
            Multiselect = false
        };

        if (dlg.ShowDialog(this) != true) return;

        try
        {
            _symbolBytes = File.ReadAllBytes(dlg.FileName);
            ShowSymbolPreview(_symbolBytes);
        }
        catch (Exception ex)
        {
            ShowError($"Could not load image: {ex.Message}");
        }
    }

    private void ClearSymbol_Click(object sender, RoutedEventArgs e)
    {
        _symbolBytes = null;
        SymbolPreview.Source = null;
        SymbolPreview.Visibility = Visibility.Collapsed;
        SymbolPlaceholder.Visibility = Visibility.Visible;
        ClearSymbolBtn.Visibility = Visibility.Collapsed;
    }

    private void ShowSymbolPreview(byte[] bytes)
    {
        try
        {
            var bmp = new BitmapImage();
            using var ms = new MemoryStream(bytes);
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze();

            SymbolPreview.Source = bmp;
            SymbolPreview.Visibility = Visibility.Visible;
            SymbolPlaceholder.Visibility = Visibility.Collapsed;
            ClearSymbolBtn.Visibility = Visibility.Visible;
        }
        catch
        {
            // If the existing image bytes are corrupt, silently ignore
            _symbolBytes = null;
        }
    }

    // ── Save / Cancel ─────────────────────────────────────────────────────────

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = NameEngBox.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ShowError("Physician name (English) is required.");
            return;
        }

        var physician = _existing ?? new Physician();
        physician.NameEng                  = name;
        physician.SpecialityEng            = SpecialityEngBox.Text.Trim();
        physician.OtherSpecialityEng       = OtherSpecialityEngBox.Text.Trim();
        physician.NameDari                 = NameDariBox.Text.Trim();
        physician.SpecialityDari           = SpecialityDariBox.Text.Trim();
        physician.OtherSpecialityDari      = OtherSpecialityDariBox.Text.Trim();
        physician.ContactNumber            = ContactBox.Text.Trim();
        physician.WhatsAppNumber           = WhatsAppBox.Text.Trim();
        physician.ReceptionContactNumber   = ReceptionBox.Text.Trim();
        physician.Address                  = AddressBox.Text.Trim();
        physician.SymbolImage              = _symbolBytes;

        bool isNew = _existing is null;
        try
        {
            using var db = _factory.CreateDbContext();
            if (isNew)
                db.Physicians.Add(physician);
            else
                db.Update(physician);
            db.SaveChanges();
            AuditService.Log(
                isNew ? "PhysicianCreated" : "PhysicianUpdated",
                "Physician", physician.Id, physician.NameEng);
        }
        catch (Exception ex)
        {
            ShowError($"Could not save physician:\n{ex.Message}");
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
