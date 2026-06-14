using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using OPDClinic.Helpers;
using OPDClinic.Models;
using OPDClinic.Services;
using OPDClinic.ViewModels;
using Serilog;

namespace OPDClinic.Views;

public partial class PatientDetailWindow : Window
{
    private readonly PatientDetailViewModel _vm;
    private bool _chartVisible;

    public PatientDetailWindow(Patient patient)
    {
        InitializeComponent();
        DialogHelper.ApplyConstraints(this);
        _vm         = new PatientDetailViewModel(App.DbFactory, patient);
        DataContext = _vm;

        // Redraw chart whenever visits reload
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(PatientDetailViewModel.Visits))
                DrawVitalsChart();
        };

        // Draw once the canvas has a real size
        VitalsCanvas.Loaded += (_, _) => DrawVitalsChart();

        // Ctrl+N → New Visit
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.N && (Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                NewVisit_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        };
    }

    // ── Export visits ─────────────────────────────────────────────────────────

    private void ExportVisits_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Auth.Can(Permission.ExportVisits)) return;
        var visits = _vm.Visits.Select(r => r.Visit).ToList();
        ExportService.ExportVisits(visits, _vm.Patient);
    }

    // ── New visit ─────────────────────────────────────────────────────────────

    private void NewVisit_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Auth.Can(Permission.RegisterPatient)) return;
        var vm  = new PatientEditViewModel(App.DbFactory, _vm.Patient);
        var win = new PatientEditWindow(vm) { Owner = this };
        if (win.ShowDialog() == true)
            _vm.LoadVisitsCommand.Execute(null);
    }

    // ── Edit existing visit ───────────────────────────────────────────────────

    private void EditVisit_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Auth.Can(Permission.EnterClinicalData)) return;
        if (sender is not Button btn || btn.Tag is not VisitListRow row) return;

        var vm  = new PatientEditViewModel(App.DbFactory, _vm.Patient, row.Visit);
        var win = new PatientEditWindow(vm) { Owner = this };
        if (win.ShowDialog() == true)
            _vm.LoadVisitsCommand.Execute(null);
    }

    // ── Repeat prescription ───────────────────────────────────────────────────

    private void RepeatVisit_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Auth.Can(Permission.RegisterPatient)) return;
        if (sender is not Button btn || btn.Tag is not VisitListRow row) return;

        using var db = App.DbFactory.CreateDbContext();
        var sourceVisit = db.Visits.Find(row.VisitId);
        if (sourceVisit is null) return;

        var vm = new PatientEditViewModel(App.DbFactory, _vm.Patient);
        vm.RepeatFromVisit(sourceVisit);

        var win = new PatientEditWindow(vm) { Owner = this };
        if (win.ShowDialog() == true)
            _vm.LoadVisitsCommand.Execute(null);
    }

    // ── Print visit ───────────────────────────────────────────────────────────

    private void PrintVisit_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Auth.Can(Permission.PrintPdf)) return;
        if (sender is not Button btn || btn.Tag is not VisitListRow row) return;

        try
        {
            var path = new PdfService(App.DbFactory).GenerateForVisit(row.VisitId);
            PrintService.OpenPdf(path);
            AuditService.Log("PrescriptionPrinted", "Visit", row.VisitId, _vm.Patient.PatientName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "PDF generation failed for VisitId={VisitId}", row.VisitId);
            MessageBox.Show($"PDF generation failed:\n{ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── Delete visit ──────────────────────────────────────────────────────────

    private void DeleteVisit_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Auth.Can(Permission.DeleteVisit)) return;
        if (sender is not Button btn || btn.Tag is not VisitListRow row) return;

        bool isLastVisit = _vm.Visits.Count == 1;

        string message = isLastVisit
            ? $"This is the only visit for '{_vm.Patient.PatientName}'.\n\n" +
              "Deleting it will permanently remove the patient record and all associated data.\n" +
              "This cannot be undone."
            : $"Delete visit from {row.DateText}?\n\n" +
              "All prescription lines and lab tests for this visit will also be deleted.\n" +
              "This cannot be undone.";

        string title = isLastVisit ? "Delete Visit & Patient Record" : "Delete Visit";

        var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            if (isLastVisit)
            {
                using var db = App.DbFactory.CreateDbContext();
                var patient = db.Patients.Find(_vm.Patient.Id);
                if (patient is not null)
                {
                    db.Patients.Remove(patient);   // cascade: visits → rx lines → labs
                    db.SaveChanges();
                    AuditService.Log("VisitDeleted",    "Visit",   row.VisitId,      _vm.Patient.PatientName);
                    AuditService.Log("PatientDeleted",  "Patient", _vm.Patient.Id,   _vm.Patient.PatientName);
                    Log.Information("LastVisitDeleted+PatientDeleted — VisitId:{Vid} PatientId:{Pid}",
                        row.VisitId, _vm.Patient.Id);
                }
                this.Close();
            }
            else
            {
                using var db = App.DbFactory.CreateDbContext();
                var visit = db.Visits.Find(row.VisitId);
                if (visit is not null)
                {
                    db.Visits.Remove(visit);
                    db.SaveChanges();
                    AuditService.Log("VisitDeleted", "Visit", row.VisitId, _vm.Patient.PatientName);
                    Log.Information("VisitDeleted — VisitId:{Id} Patient:{Name}", row.VisitId, _vm.Patient.PatientName);
                }
                _vm.LoadVisitsCommand.Execute(null);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Delete visit failed for VisitId={VisitId}", row.VisitId);
            MessageBox.Show($"Delete failed:\n{ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── Delete patient (empty-state button) ───────────────────────────────────

    private void DeletePatient_Click(object sender, RoutedEventArgs e)
    {
        if (!App.Auth.Can(Permission.DeletePatient)) return;

        var result = MessageBox.Show(
            $"Permanently delete patient '{_vm.Patient.PatientName}'?\n\n" +
            "All visits, prescriptions, and lab results for this patient will also be deleted.\n" +
            "This cannot be undone.",
            "Delete Patient Record",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            using var db = App.DbFactory.CreateDbContext();
            var patient = db.Patients.Find(_vm.Patient.Id);
            if (patient is not null)
            {
                db.Patients.Remove(patient);   // cascade: visits → rx lines → labs
                db.SaveChanges();
                AuditService.Log("PatientDeleted", "Patient", _vm.Patient.Id, _vm.Patient.PatientName);
                Log.Information("PatientDeleted — PatientId:{Id} Name:{Name}", _vm.Patient.Id, _vm.Patient.PatientName);
            }
            this.Close();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Delete patient failed for PatientId={PatientId}", _vm.Patient.Id);
            MessageBox.Show($"Delete failed:\n{ex.Message}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── Timeline expand / collapse ────────────────────────────────────────────

    private void ToggleDetails_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is VisitListRow row)
            row.IsExpanded = !row.IsExpanded;
    }

    // ── Vitals chart toggle ───────────────────────────────────────────────────

    private void ChartToggle_Click(object sender, RoutedEventArgs e)
    {
        _chartVisible = !_chartVisible;
        ChartPanel.Visibility = _chartVisible ? Visibility.Visible : Visibility.Collapsed;
        ChartToggleBtn.Content = _chartVisible ? "▴ Hide Trend" : "▾ Vitals Trend";

        if (_chartVisible)
            // Canvas may have just become visible — ensure it has a real size before drawing
            Dispatcher.InvokeAsync(DrawVitalsChart, System.Windows.Threading.DispatcherPriority.Render);
    }

    private void VitalsCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => DrawVitalsChart();

    // ── Vitals trend chart drawing ────────────────────────────────────────────

    private void DrawVitalsChart()
    {
        VitalsCanvas.Children.Clear();

        if (!_chartVisible) return;

        var history = _vm.VitalsHistory;
        var w = VitalsCanvas.ActualWidth;
        var h = VitalsCanvas.ActualHeight;

        if (history.Count < 2 || w < 20 || h < 20) return;

        // Padding: left / right / top / bottom
        const double pL = 10, pR = 10, pT = 10, pB = 22;
        var plotW = w - pL - pR;
        var plotH = h - pT - pB;

        // ── Background grid lines ─────────────────────────────────────────────
        var gridBrush = new SolidColorBrush(Color.FromRgb(229, 231, 235)); // gray-200
        for (int i = 0; i <= 4; i++)
        {
            var y = pT + i * plotH / 4;
            VitalsCanvas.Children.Add(new Line
            {
                X1 = pL, X2 = w - pR, Y1 = y, Y2 = y,
                Stroke = gridBrush, StrokeThickness = 0.5
            });
        }

        // ── Date mapping (X axis) ─────────────────────────────────────────────
        var datesWithData = history.Where(p => p.Date != default).ToList();
        if (datesWithData.Count < 2) return;

        var minTick = datesWithData.Min(p => p.Date.Ticks);
        var maxTick = datesWithData.Max(p => p.Date.Ticks);
        var tickRange = maxTick - minTick;

        double MapX(DateTime d) =>
            tickRange == 0 ? pL + plotW / 2 : pL + (d.Ticks - minTick) / (double)tickRange * plotW;

        // ── Draw a series ─────────────────────────────────────────────────────
        void DrawSeries(
            IEnumerable<(DateTime Date, double Value)> pts,
            Color lineColor,
            Color dotColor)
        {
            var sorted = pts.OrderBy(p => p.Date).ToList();
            if (sorted.Count < 2) return;

            var minV = sorted.Min(p => p.Value);
            var maxV = sorted.Max(p => p.Value);
            var vRange = maxV - minV;

            double MapY(double v) =>
                vRange < 0.001 ? pT + plotH / 2
                               : pT + (1 - (v - minV) / vRange) * plotH * 0.8 + plotH * 0.1;

            var poly = new Polyline
            {
                Stroke          = new SolidColorBrush(lineColor),
                StrokeThickness = 2,
                StrokeLineJoin  = PenLineJoin.Round
            };

            foreach (var pt in sorted)
            {
                var x = MapX(pt.Date);
                var y = MapY(pt.Value);
                poly.Points.Add(new Point(x, y));

                var dot = new Ellipse
                {
                    Width = 7, Height = 7,
                    Fill  = new SolidColorBrush(dotColor),
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 1.5
                };
                Canvas.SetLeft(dot, x - 3.5);
                Canvas.SetTop(dot,  y - 3.5);
                VitalsCanvas.Children.Add(dot);
            }

            // Insert polyline before the dots so dots render on top
            VitalsCanvas.Children.Insert(0, poly);
        }

        // BW series (blue)
        var bwPts = history
            .Where(p => p.Bw.HasValue)
            .Select(p => (p.Date, p.Bw!.Value));
        DrawSeries(bwPts, Color.FromRgb(21, 101, 192), Color.FromRgb(21, 101, 192));

        // Systolic BP series (red)
        var bpPts = history
            .Where(p => p.SysBp.HasValue)
            .Select(p => (p.Date, p.SysBp!.Value));
        DrawSeries(bpPts, Color.FromRgb(198, 40, 40), Color.FromRgb(198, 40, 40));

        // ── X-axis date labels (first and last) ───────────────────────────────
        var labelBrush = new SolidColorBrush(Color.FromRgb(107, 114, 128)); // gray-500

        void AddDateLabel(DateTime d, double x, HorizontalAlignment align)
        {
            var tb = new TextBlock
            {
                Text       = d.ToString("MMM yy"),
                FontSize   = 10,
                Foreground = labelBrush
            };
            tb.Measure(new Size(80, 20));
            var offset = align == HorizontalAlignment.Left ? 0 : -tb.DesiredSize.Width;
            Canvas.SetLeft(tb, x + offset);
            Canvas.SetTop(tb,  h - pB + 4);
            VitalsCanvas.Children.Add(tb);
        }

        var firstDate = datesWithData.Min(p => p.Date);
        var lastDate  = datesWithData.Max(p => p.Date);
        AddDateLabel(firstDate, MapX(firstDate), HorizontalAlignment.Left);

        // Only add last label if it's far enough from the first to avoid overlap
        if (MapX(lastDate) - MapX(firstDate) > 60)
            AddDateLabel(lastDate, MapX(lastDate), HorizontalAlignment.Right);
    }
}
