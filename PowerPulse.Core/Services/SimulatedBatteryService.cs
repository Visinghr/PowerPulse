using PowerPulse.Core.Models;

namespace PowerPulse.Core.Services;

/// <summary>
/// Simulated battery service for testing on desktops without a battery.
/// Simulates a discharging battery that drains over time with realistic values.
/// </summary>
public class SimulatedBatteryService : IBatteryService, IDisposable
{
    private System.Threading.Timer? _pollingTimer;
    private Action<BatteryInfo>? _onUpdate;

    private double _simulatedPercent = 85.0;
    private int _simulatedRemainingMWh = 42500; // ~42.5 Wh
    private readonly Random _random = new();
    private bool _isCharging;
    private int _tickCount;

    // Simulated battery spec (Surface Laptop-like)
    private const int DesignCapacity = 50000;   // 50 Wh
    private const int FullChargeCapacity = 47000; // 47 Wh (some degradation)
    private const int BaseDischargeRate = 8500;  // ~8.5W average
    private const int BaseVoltage = 7700;        // 7.7V

    public BatteryInfo GetBatteryInfo()
    {
        _tickCount++;

        // Simulate varying discharge rate (7-12W with noise)
        int rateNoise = _random.Next(-1500, 2500);
        int dischargeRate = BaseDischargeRate + rateNoise;

        // Every 60 ticks (~3 min), toggle charging simulation
        if (_tickCount % 60 == 0)
            _isCharging = !_isCharging;

        // Drain battery slowly
        if (!_isCharging)
        {
            _simulatedRemainingMWh -= (int)(dischargeRate * 3.0 / 3600.0); // 3s tick
            _simulatedPercent = (double)_simulatedRemainingMWh / FullChargeCapacity * 100;
            if (_simulatedPercent < 5)
            {
                _simulatedPercent = 85;
                _simulatedRemainingMWh = 42500;
            }
        }
        else
        {
            _simulatedRemainingMWh += (int)(15000 * 3.0 / 3600.0); // charge at ~15W
            _simulatedPercent = (double)_simulatedRemainingMWh / FullChargeCapacity * 100;
            if (_simulatedPercent > 100)
            {
                _simulatedPercent = 100;
                _simulatedRemainingMWh = FullChargeCapacity;
            }
        }

        // Voltage varies slightly
        int voltage = BaseVoltage + _random.Next(-100, 100);

        var status = _isCharging ? BatteryPowerStatus.Charging
            : _simulatedPercent < 10 ? BatteryPowerStatus.Critical
            : BatteryPowerStatus.Discharging;

        return new BatteryInfo
        {
            Percentage = (int)Math.Round(_simulatedPercent),
            Status = status,
            DischargeRateMW = _isCharging ? 0 : dischargeRate,
            ChargeRateMW = _isCharging ? 15000 : 0,
            RemainingCapacityMWh = _simulatedRemainingMWh,
            FullChargeCapacityMWh = FullChargeCapacity,
            DesignCapacityMWh = DesignCapacity,
            VoltageMV = voltage,
            IsBatteryPresent = true,
            IsAcConnected = _isCharging,
            Timestamp = DateTime.Now
        };
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
            catch { }
        }, null, 0, intervalMs);
    }

    public void StopMonitoring()
    {
        _pollingTimer?.Dispose();
        _pollingTimer = null;
    }

    public bool IsSupported() => true; // Always works — it's simulated

    public void Dispose()
    {
        StopMonitoring();
        GC.SuppressFinalize(this);
    }
}
