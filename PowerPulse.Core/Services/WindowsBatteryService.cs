using PowerPulse.Core.Models;
using Windows.Devices.Power;
using BatteryStatusEnum = Windows.System.Power.BatteryStatus;

namespace PowerPulse.Core.Services;

/// <summary>
/// Primary battery service using Windows.Devices.Power.BatteryReport API.
/// Provides design/full/remaining capacity and charge/discharge rate.
/// Confirmed working on Surface Laptop 4, Surface Pro 8.
/// </summary>
public class WindowsBatteryService : IBatteryService, IDisposable
{
    private System.Threading.Timer? _pollingTimer;
    private Action<BatteryInfo>? _onUpdate;
    private readonly Battery _aggregateBattery;

    public WindowsBatteryService()
    {
        _aggregateBattery = Battery.AggregateBattery;
    }

    public BatteryInfo GetBatteryInfo()
    {
        var report = _aggregateBattery.GetReport();
        return MapReport(report);
    }

    public void StartMonitoring(Action<BatteryInfo> onUpdate, int intervalMs = 3000)
    {
        _onUpdate = onUpdate;

        // Subscribe to event-driven updates as well
        _aggregateBattery.ReportUpdated += OnReportUpdated;

        // Also poll on interval as a supplement (spec recommends 2-5s)
        _pollingTimer = new System.Threading.Timer(_ =>
        {
            try
            {
                var info = GetBatteryInfo();
                _onUpdate?.Invoke(info);
            }
            catch
            {
                // Swallow errors during polling — don't crash the app
            }
        }, null, 0, intervalMs);
    }

    public void StopMonitoring()
    {
        _aggregateBattery.ReportUpdated -= OnReportUpdated;
        _pollingTimer?.Dispose();
        _pollingTimer = null;
        _onUpdate = null;
    }

    public bool IsSupported()
    {
        try
        {
            var report = _aggregateBattery.GetReport();
            return report.Status != BatteryStatusEnum.NotPresent
                && report.RemainingCapacityInMilliwattHours.HasValue;
        }
        catch
        {
            return false;
        }
    }

    private void OnReportUpdated(Battery sender, object args)
    {
        try
        {
            var report = sender.GetReport();
            var info = MapReport(report);
            _onUpdate?.Invoke(info);
        }
        catch
        {
            // Swallow — event-driven updates are supplemental
        }
    }

    private static BatteryInfo MapReport(BatteryReport report)
    {
        int remaining = report.RemainingCapacityInMilliwattHours ?? 0;
        int fullCharge = report.FullChargeCapacityInMilliwattHours ?? 0;
        int design = report.DesignCapacityInMilliwattHours ?? 0;
        int chargeRate = report.ChargeRateInMilliwatts ?? 0;

        // ChargeRateInMilliwatts: negative = discharging, positive = charging
        int dischargeRateMW = chargeRate < 0 ? Math.Abs(chargeRate) : 0;
        int chargeRateMW = chargeRate > 0 ? chargeRate : 0;

        int percentage = fullCharge > 0
            ? (int)Math.Round((double)remaining / fullCharge * 100)
            : 0;
        percentage = Math.Clamp(percentage, 0, 100);

        var status = report.Status switch
        {
            BatteryStatusEnum.Discharging => BatteryPowerStatus.Discharging,
            BatteryStatusEnum.Charging => BatteryPowerStatus.Charging,
            BatteryStatusEnum.Idle => BatteryPowerStatus.Idle,
            BatteryStatusEnum.NotPresent => BatteryPowerStatus.NotPresent,
            _ => BatteryPowerStatus.Unknown
        };

        return new BatteryInfo
        {
            Percentage = percentage,
            Status = status,
            DischargeRateMW = dischargeRateMW,
            ChargeRateMW = chargeRateMW,
            RemainingCapacityMWh = remaining,
            FullChargeCapacityMWh = fullCharge,
            DesignCapacityMWh = design,
            VoltageMV = null, // Windows.Devices.Power doesn't provide voltage
            IsBatteryPresent = report.Status != BatteryStatusEnum.NotPresent,
            IsAcConnected = report.Status == BatteryStatusEnum.Charging
                         || report.Status == BatteryStatusEnum.Idle,
            Timestamp = DateTime.Now
        };
    }

    public void Dispose()
    {
        StopMonitoring();
        GC.SuppressFinalize(this);
    }
}
