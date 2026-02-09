using System.Windows;
using PowerPulse.UI.Services;
using PowerPulse.UI.ViewModels;

namespace PowerPulse.UI;

/// <summary>
/// Application startup. Initializes tray icon and main window.
/// </summary>
public partial class App : Application
{
    private TrayIconService? _trayService;

    /// <summary>
    /// Whether the tray icon is active (controls close-to-tray behavior).
    /// </summary>
    public static bool TrayIconActive { get; set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = new Views.MainWindow();
        MainWindow = mainWindow;

        // Set up tray icon
        _trayService = new TrayIconService(mainWindow);
        _trayService.Initialize();
        TrayIconActive = true;

        // Update tray tooltip when battery info changes
        var vm = (MainViewModel)mainWindow.DataContext;
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.BatteryPercentText)
                || args.PropertyName == nameof(MainViewModel.TimeRemainingText))
            {
                var tooltip = $"PowerPulse: {vm.BatteryPercentText} — {vm.TimeRemainingText}";
                _trayService?.UpdateTooltip(tooltip);
            }
        };

        // Handle notification requests from ViewModel
        vm.NotificationRequested += (_, notification) =>
        {
            _trayService?.ShowNotification(notification.Title, notification.Message, notification.Icon);
        };

        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _trayService?.Dispose();
        base.OnExit(e);
    }
}

