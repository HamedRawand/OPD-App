using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using OPDClinic.Data;
using OPDClinic.Helpers;
using OPDClinic.Models;

namespace OPDClinic.Views;

public partial class RouteEditDialog : Window
{
    private readonly IDbContextFactory<AppDbContext> _factory;
    private readonly RouteOfAdministration? _existing;

    public RouteEditDialog(IDbContextFactory<AppDbContext> factory, RouteOfAdministration? route = null)
    {
        InitializeComponent();
        DialogHelper.ApplyConstraints(this);
        _factory  = factory;
        _existing = route;

        if (route is not null)
        {
            HeaderTitle.SetResourceReference(TextBlock.TextProperty, "Options.RouteEdit.Header.Edit");
            CategoryBox.Text     = route.Category     ?? "";
            RouteNameBox.Text    = route.RouteName    ?? "";
            AbbreviationBox.Text = route.Abbreviation ?? "";
            DescriptionBox.Text  = route.Description  ?? "";
        }
        else
        {
            HeaderTitle.SetResourceReference(TextBlock.TextProperty, "Options.RouteEdit.Header.Add");
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var name = RouteNameBox.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            ShowError("Route name is required.");
            return;
        }

        var item = _existing ?? new RouteOfAdministration();
        item.Category     = CategoryBox.Text.NullIfEmpty();
        item.RouteName    = name;
        item.Abbreviation = AbbreviationBox.Text.NullIfEmpty();
        item.Description  = DescriptionBox.Text.NullIfEmpty();

        try
        {
            using var db = _factory.CreateDbContext();
            if (_existing is null)
                db.Routes.Add(item);
            else
                db.Update(item);
            db.SaveChanges();
        }
        catch (Exception ex)
        {
            ShowError($"Could not save route:\n{ex.Message}");
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
