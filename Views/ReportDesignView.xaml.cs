using System.Windows.Controls;
using OPDClinic.ViewModels;

namespace OPDClinic.Views;

public partial class ReportDesignView : UserControl
{
    public ReportDesignViewModel ViewModel { get; }

    public ReportDesignView()
    {
        InitializeComponent();
        ViewModel = new ReportDesignViewModel();
        DataContext = ViewModel;
        ViewModel.Load();
    }
}
