namespace PowerPulse.Core.Models;

/// <summary>
/// Represents the current state of the system battery.
/// </summary>
public class BatteryInfo
{
    /// <summary>Battery charge percentage (0-100).</summary>
    public int Percentage { get; set; }

    /// <summary>Current battery status.</summary>
    public BatteryPowerStatus Status { get; set; } = BatteryPowerStatus.Unknown;

    /// <summary>Current discharge rate in milliwatts (positive when discharging).</summary>
    public int DischargeRateMW { get; set; }

    /// <summary>Current charge rate in milliwatts (positive when charging).</summary>
    public int ChargeRateMW { get; set; }

    /// <summary>Remaining battery capacity in milliwatt-hours.</summary>
    public int RemainingCapacityMWh { get; set; }

    /// <summary>Current full charge capacity in milliwatt-hours (degrades over time).</summary>
    public int FullChargeCapacityMWh { get; set; }

    /// <summary>Original design capacity in milliwatt-hours.</summary>
    public int DesignCapacityMWh { get; set; }

    /// <summary>Current voltage in millivolts, if available.</summary>
    public int? VoltageMV { get; set; }

    /// <summary>Battery health as percentage: (FullCharge / Design) × 100.</summary>
    public double HealthPercent =>
        DesignCapacityMWh > 0
            ? Math.Round((double)FullChargeCapacityMWh / DesignCapacityMWh * 100, 1)
            : 0;

    /// <summary>Estimated time remaining on battery, calculated via EMA-smoothed rate.</summary>
    public TimeSpan? EstimatedTimeRemaining { get; set; }

    /// <summary>Timestamp when this reading was taken.</summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>Whether the battery is present in the system.</summary>
    public bool IsBatteryPresent { get; set; }

    /// <summary>Whether AC power is connected.</summary>
    public bool IsAcConnected { get; set; }
}

/// <summary>
/// Battery power status enumeration.
/// </summary>
public enum BatteryPowerStatus
{
    Unknown,
    Discharging,
    Charging,
    Idle,
    NotPresent,
    Critical
}
