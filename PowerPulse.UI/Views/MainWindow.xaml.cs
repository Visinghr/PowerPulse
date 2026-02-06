using System.ComponentModel;
using System.Windows;
using PowerPulse.UI.ViewModels;

namespace PowerPulse.UI.Views;

public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.Initialize();

        // Update the battery arc gauge when percent changes
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        UpdateBatteryArc(ViewModel.BatteryPercent);
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.BatteryPercent))
        {
            UpdateBatteryArc(ViewModel.BatteryPercent);
        }
    }

    /// <summary>
    /// Updates the circular gauge StrokeDashArray to represent battery percentage.
    /// The ellipse circumference in StrokeDashArray units ≈ 3.14 (for a normalized circle).
    /// </summary>
    private void UpdateBatteryArc(int percent)
    {
        double fraction = Math.Clamp(percent / 100.0, 0, 1);
        double dashLength = 3.14159 * fraction;
        double gapLength = 3.14159 * (1 - fraction) + 0.01;

        BatteryArc.StrokeDashArray = new System.Windows.Media.DoubleCollection { dashLength, gapLength };
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        // Minimize to tray instead of closing (if tray icon is active)
        if (App.TrayIconActive)
        {
            e.Cancel = true;
            Hide();
            return;
        }

        // Actually closing — clean up
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        ViewModel.Dispose();
    }
}
