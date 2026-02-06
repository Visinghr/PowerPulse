using System.Management;
using PowerPulse.Core.Models;

namespace PowerPulse.Core.Services;

/// <summary>
/// Fallback battery service using WMI queries.
/// Provides discharge rate, voltage, and capacity from root\wmi BatteryStatus.
/// Used when WindowsBatteryService returns incomplete data,
/// or to supplement with voltage information.
/// </summary>
public class WmiBatteryService : IBatteryService, IDisposable
{
    private System.Threading.Timer? _pollingTimer;
    private Action<BatteryInfo>? _onUpdate;

    public BatteryInfo GetBatteryInfo()
    {
        var info = new BatteryInfo { Timestamp = DateTime.Now };

        try
        {
            // Query real-time battery status (root\wmi)
            using var statusSearcher = new ManagementObjectSearcher("root\\wmi", "SELECT * FROM BatteryStatus");
            foreach (var obj in statusSearcher.Get())
            {
                info.IsBatteryPresent = true;

                bool discharging = Convert.ToBoolean(obj["Discharging"]);
                bool charging = Convert.ToBoolean(obj["Charging"]);
                bool powerOnline = Convert.ToBoolean(obj["PowerOnline"]);
                bool critical = Convert.ToBoolean(obj["Critical"]);

                int dischargeRate = Convert.ToInt32(obj["DischargeRate"]);
                int remainingCapacity = Convert.ToInt32(obj["RemainingCapacity"]);
                int voltage = Convert.ToInt32(obj["Voltage"]);

                info.RemainingCapacityMWh = remainingCapacity;
                info.VoltageMV = voltage;
                info.IsAcConnected = powerOnline;

                if (discharging)
                {
                    info.Status = critical ? BatteryPowerStatus.Critical : BatteryPowerStatus.Discharging;
                    info.DischargeRateMW = dischargeRate;
                    info.ChargeRateMW = 0;
                }
                else if (charging)
                {
                    info.Status = BatteryPowerStatus.Charging;
                    info.ChargeRateMW = dischargeRate; // WMI reports rate in same field
                    info.DischargeRateMW = 0;
                }
                else
                {
                    info.Status = BatteryPowerStatus.Idle;
                    info.DischargeRateMW = 0;
                    info.ChargeRateMW = 0;
                }

                break; // Use first battery
            }

            // Query full charge capacity
            using var fullChargeSearcher = new ManagementObjectSearcher("root\\wmi", "SELECT * FROM BatteryFullChargedCapacity");
            foreach (var obj in fullChargeSearcher.Get())
            {
                info.FullChargeCapacityMWh = Convert.ToInt32(obj["FullChargedCapacity"]);
                break;
            }

            // Query design capacity from static data
            using var staticSearcher = new ManagementObjectSearcher("root\\wmi", "SELECT * FROM BatteryStaticData");
            foreach (var obj in staticSearcher.Get())
            {
                info.DesignCapacityMWh = Convert.ToInt32(obj["DesignedCapacity"]);
                break;
            }

            // Calculate percentage
            if (info.FullChargeCapacityMWh > 0)
            {
                info.Percentage = (int)Math.Round((double)info.RemainingCapacityMWh / info.FullChargeCapacityMWh * 100);
                info.Percentage = Math.Clamp(info.Percentage, 0, 100);
            }
        }
        catch (ManagementException)
        {
            info.IsBatteryPresent = false;
            info.Status = BatteryPowerStatus.NotPresent;
        }

        return info;
    }

    public void StartMonitoring(Action<BatteryInfo> onUpdate, int intervalMs = 3000)
    {
        _onUpdate = onUpdate;
        _pollingTimer = new System.Threading.Timer(_ =>
        {
            try
            {
                var info = GetBatteryInfo();
                _onUpdate?.Invoke(info);
            }
            catch
            {
                // Swallow errors during polling
            }
        }, null, 0, intervalMs);
    }

    public void StopMonitoring()
    {
        _pollingTimer?.Dispose();
        _pollingTimer = null;
        _onUpdate = null;
    }

    public bool IsSupported()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher("root\\wmi", "SELECT * FROM BatteryStatus");
            var results = searcher.Get();
            return results.Count > 0;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        StopMonitoring();
        GC.SuppressFinalize(this);
    }
}
