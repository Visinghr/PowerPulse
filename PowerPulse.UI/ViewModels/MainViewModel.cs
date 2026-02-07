using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using PowerPulse.Core.Models;
using PowerPulse.Core.Services;
using PowerPulse.Core.Utilities;

namespace PowerPulse.UI.ViewModels;

/// <summary>
/// Main ViewModel for the battery dashboard. Observes BatteryMonitoringService
/// and exposes formatted properties for the UI to bind to.
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly BatteryMonitoringService _monitoringService;
    private readonly Dispatcher _dispatcher;

    [ObservableProperty] private int _batteryPercent;
    [ObservableProperty] private string _batteryPercentText = "—%";
    [ObservableProperty] private string _statusText = "Detecting...";
    [ObservableProperty] private string _timeRemainingText = "Calculating...";
    [ObservableProperty] private string _dischargeRateText = "— W";
    [ObservableProperty] private string _chargeRateText = "— W";
    [ObservableProperty] private string _healthPercentText = "—%";
    [ObservableProperty] private string _healthColor = "#4CAF50"; // Green default
    [ObservableProperty] private string _designCapacityText = "— Wh";
    [ObservableProperty] private string _fullChargeCapacityText = "— Wh";
    [ObservableProperty] private string _remainingCapacityText = "— Wh";
    [ObservableProperty] private string _voltageText = "— V";
    [ObservableProperty] private string _activeApisText = "Detecting...";
    [ObservableProperty] private bool _isBatteryPresent = true;
    [ObservableProperty] private bool _isCharging;
    [ObservableProperty] private bool _isDischarging;
    [ObservableProperty] private string _powerRateLabel = "Discharge Rate";
    [ObservableProperty] private string _powerRateText = "— W";
    [ObservableProperty] private string _statusIcon = "🔋";

    /// <summary>Current raw battery info for tray icon tooltip.</summary>
    public BatteryInfo? LatestInfo { get; private set; }

    public MainViewModel()
    {
        _monitoringService = new BatteryMonitoringService();
        _dispatcher = Dispatcher.CurrentDispatcher;
    }

    /// <summary>
    /// Initializes monitoring. Call from UI thread after window loads.
    /// Runs detection on a background thread to avoid blocking the UI.
    /// </summary>
    public void Initialize()
    {
        StatusText = "Detecting battery APIs...";

        Task.Run(() =>
        {
            try
            {
                Log("Detecting capabilities...");
                _monitoringService.DetectCapabilities();
                Log($"APIs available: {_monitoringService.ActiveApis}");

                _dispatcher.BeginInvoke(() =>
                {
                    ActiveApisText = _monitoringService.ActiveApis;

                    if (!_monitoringService.IsBatteryAvailable)
                    {
                        Log("No battery detected on this system.");
                        IsBatteryPresent = false;
                        StatusText = "No battery detected";
                        StatusIcon = "🔌";
                        return;
                    }

                    if (_monitoringService.IsSimulated)
                    {
                        Log("Simulated battery mode active.");
                        ActiveApisText = "⚠ Simulated Battery";
                    }

                    Log("Starting monitoring (3s interval)...");
                    _monitoringService.StartMonitoring(OnBatteryUpdate, intervalMs: 3000);
                });
            }
            catch (Exception ex)
            {
                Log($"Init FAILED: {ex}");
                _dispatcher.BeginInvoke(() =>
                {
                    StatusText = $"Error: {ex.Message}";
                    ActiveApisText = "Detection failed";
                });
            }
        });
    }

    private void OnBatteryUpdate(BatteryInfo info)
    {
        // Marshal to UI thread using captured dispatcher
        _dispatcher.BeginInvoke(() =>
        {
            try
            {
                UpdateFromInfo(info);
            }
            catch (Exception ex)
            {
                Log($"Update error: {ex}");
                StatusText = $"Update error: {ex.Message}";
            }
        });
    }

    private void UpdateFromInfo(BatteryInfo info)
    {
        _logUpdateCount++;
        // Log every 10th update (every ~30 seconds) to avoid spam
        if (_logUpdateCount <= 3 || _logUpdateCount % 10 == 0)
        {
            Log($"Update #{_logUpdateCount}: {info.Percentage}% | Status={info.Status} | " +
                $"DischargeRate={info.DischargeRateMW}mW | ChargeRate={info.ChargeRateMW}mW | " +
                $"Remaining={info.RemainingCapacityMWh}mWh | FullCharge={info.FullChargeCapacityMWh}mWh | " +
                $"Design={info.DesignCapacityMWh}mWh | Voltage={info.VoltageMV}mV | " +
                $"Health={info.HealthPercent:F1}% | ETA={info.EstimatedTimeRemaining}");
        }

        LatestInfo = info;

        if (!info.IsBatteryPresent)
        {
            IsBatteryPresent = false;
            StatusText = "No battery detected";
            StatusIcon = "🔌";
            return;
        }

        IsBatteryPresent = true;
        BatteryPercent = info.Percentage;
        BatteryPercentText = $"{info.Percentage}%";

        // Status
        IsCharging = info.Status == BatteryPowerStatus.Charging;
        IsDischarging = info.Status == BatteryPowerStatus.Discharging || info.Status == BatteryPowerStatus.Critical;

        StatusText = info.Status switch
        {
            BatteryPowerStatus.Discharging => "On Battery",
            BatteryPowerStatus.Charging => "Charging",
            BatteryPowerStatus.Idle => "Plugged In (Full)",
            BatteryPowerStatus.Critical => "Critical — Plug In!",
            BatteryPowerStatus.NotPresent => "No Battery",
            _ => "Unknown"
        };

        StatusIcon = info.Status switch
        {
            BatteryPowerStatus.Charging => "⚡",
            BatteryPowerStatus.Idle => "🔌",
            BatteryPowerStatus.Critical => "🪫",
            BatteryPowerStatus.Discharging when info.Percentage > 50 => "🔋",
            BatteryPowerStatus.Discharging => "🪫",
            _ => "🔋"
        };

        // Power rate — show discharge or charge depending on state
        if (IsDischarging)
        {
            PowerRateLabel = "Discharge Rate";
            PowerRateText = UnitConverter.FormatWatts(UnitConverter.MilliwattsToWatts(info.DischargeRateMW));
            DischargeRateText = PowerRateText;
            ChargeRateText = "— W";
        }
        else if (IsCharging)
        {
            PowerRateLabel = "Charge Rate";
            PowerRateText = UnitConverter.FormatWatts(UnitConverter.MilliwattsToWatts(info.ChargeRateMW));
            ChargeRateText = PowerRateText;
            DischargeRateText = "— W";
        }
        else
        {
            PowerRateLabel = "Power Rate";
            PowerRateText = "0 W";
            DischargeRateText = "0 W";
            ChargeRateText = "0 W";
        }

        // Time remaining
        TimeRemainingText = IsDischarging
            ? UnitConverter.FormatTimeRemaining(info.EstimatedTimeRemaining)
            : IsCharging ? "Charging..." : "Plugged In";

        // Health
        double health = info.HealthPercent;
        HealthPercentText = health > 0 ? $"{health:F1}%" : "N/A";
        HealthColor = health switch
        {
            >= 80 => "#4CAF50", // Green
            >= 60 => "#FF9800", // Amber
            > 0 => "#F44336",   // Red
            _ => "#9E9E9E"      // Grey (unknown)
        };

        // Capacities
        DesignCapacityText = info.DesignCapacityMWh > 0
            ? $"{UnitConverter.MilliwattHoursToWattHours(info.DesignCapacityMWh)} Wh"
            : "N/A";
        FullChargeCapacityText = info.FullChargeCapacityMWh > 0
            ? $"{UnitConverter.MilliwattHoursToWattHours(info.FullChargeCapacityMWh)} Wh"
            : "N/A";
        RemainingCapacityText = info.RemainingCapacityMWh > 0
            ? $"{UnitConverter.MilliwattHoursToWattHours(info.RemainingCapacityMWh)} Wh"
            : "N/A";

        // Voltage
        VoltageText = info.VoltageMV.HasValue && info.VoltageMV > 0
            ? $"{UnitConverter.MillivoltsToVolts(info.VoltageMV.Value)} V"
            : "N/A";
    }

    public void Dispose()
    {
        _monitoringService.Dispose();
        GC.SuppressFinalize(this);
    }

    private static int _logUpdateCount;
    private static readonly string LogFile = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PowerPulse", "debug.log");

    internal static void Log(string message)
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(LogFile)!;
            if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
            var line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}";
            System.IO.File.AppendAllText(LogFile, line);
        }
        catch { /* logging should never crash the app */ }
    }
}
