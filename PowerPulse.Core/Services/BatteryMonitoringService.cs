using System.Diagnostics;
using PowerPulse.Core.Models;
using PowerPulse.Core.Utilities;

namespace PowerPulse.Core.Services;

/// <summary>
/// Orchestrates battery monitoring by combining data from multiple sources.
/// Uses WindowsBatteryService (primary) with WmiBatteryService (fallback/supplement).
/// Runs the EMA time estimator and fires unified update callbacks.
/// </summary>
public class BatteryMonitoringService : IDisposable
{
    private readonly WindowsBatteryService _windowsService;
    private readonly WmiBatteryService _wmiService;
    private readonly BatteryCalculations _estimator;

    private System.Threading.Timer? _pollingTimer;
    private Action<BatteryInfo>? _onUpdate;
    private bool _useWindowsApi;
    private bool _useWmiApi;

    private const int DefaultIntervalMs = 3000; // 3 seconds per spec

    public BatteryMonitoringService()
    {
        _windowsService = new WindowsBatteryService();
        _wmiService = new WmiBatteryService();
        _estimator = new BatteryCalculations();
    }

    /// <summary>
    /// Detects which APIs are available on this hardware.
    /// Call once at startup.
    /// </summary>
    public void DetectCapabilities()
    {
        _useWindowsApi = _windowsService.IsSupported();
        _useWmiApi = _wmiService.IsSupported();
    }

    /// <summary>Whether any battery API is available.</summary>
    public bool IsBatteryAvailable => _useWindowsApi || _useWmiApi;

    /// <summary>Which APIs are active.</summary>
    public string ActiveApis =>
        (_useWindowsApi, _useWmiApi) switch
        {
            (true, true) => "Windows.Devices.Power + WMI",
            (true, false) => "Windows.Devices.Power",
            (false, true) => "WMI BatteryStatus",
            _ => "None"
        };

    /// <summary>
    /// Starts polling for battery data and calling onUpdate on each tick.
    /// </summary>
    public void StartMonitoring(Action<BatteryInfo> onUpdate, int intervalMs = DefaultIntervalMs)
    {
        _onUpdate = onUpdate;

        _pollingTimer = new System.Threading.Timer(_ =>
        {
            try
            {
                var info = GetMergedBatteryInfo();
                _onUpdate?.Invoke(info);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[PowerPulse] Polling error: {ex}");
            }
        }, null, 0, intervalMs);
    }

    /// <summary>
    /// Gets a single merged battery reading from all available sources.
    /// </summary>
    public BatteryInfo GetMergedBatteryInfo()
    {
        BatteryInfo info;

        if (_useWindowsApi)
        {
            info = _windowsService.GetBatteryInfo();

            // Supplement with WMI data for voltage (Windows API doesn't provide it)
            if (_useWmiApi)
            {
                try
                {
                    var wmiInfo = _wmiService.GetBatteryInfo();
                    info.VoltageMV = wmiInfo.VoltageMV;

                    // If Windows API returned 0 discharge rate, try WMI
                    if (info.DischargeRateMW == 0 && wmiInfo.DischargeRateMW > 0)
                        info.DischargeRateMW = wmiInfo.DischargeRateMW;
                    if (info.ChargeRateMW == 0 && wmiInfo.ChargeRateMW > 0)
                        info.ChargeRateMW = wmiInfo.ChargeRateMW;
                }
                catch
                {
                    // WMI supplement failed — that's OK, primary data is available
                }
            }
        }
        else if (_useWmiApi)
        {
            info = _wmiService.GetBatteryInfo();
        }
        else
        {
            // No battery APIs available
            info = new BatteryInfo
            {
                IsBatteryPresent = false,
                Status = BatteryPowerStatus.NotPresent,
                Timestamp = DateTime.Now
            };
        }

        // Run EMA estimator for time remaining
        if (info.Status == BatteryPowerStatus.Discharging || info.Status == BatteryPowerStatus.Critical)
        {
            info.EstimatedTimeRemaining = _estimator.UpdateAndEstimate(
                info.DischargeRateMW,
                info.RemainingCapacityMWh);
        }
        else
        {
            _estimator.Reset();
            info.EstimatedTimeRemaining = null;
        }

        return info;
    }

    /// <summary>Stops monitoring.</summary>
    public void StopMonitoring()
    {
        _pollingTimer?.Dispose();
        _pollingTimer = null;
        _onUpdate = null;
    }

    public void Dispose()
    {
        StopMonitoring();
        _windowsService.Dispose();
        _wmiService.Dispose();
        GC.SuppressFinalize(this);
    }
}
