namespace PowerPulse.Core.Models;

/// <summary>
/// Represents a single power consumption data point for historical tracking.
/// </summary>
public class PowerSample
{
    /// <summary>Timestamp of this sample.</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>Battery percentage at this time (0-100).</summary>
    public int Percentage { get; set; }

    /// <summary>Discharge rate in milliwatts (0 if charging/idle).</summary>
    public int DischargeRateMW { get; set; }

    /// <summary>Charge rate in milliwatts (0 if discharging/idle).</summary>
    public int ChargeRateMW { get; set; }

    /// <summary>Battery status at this time.</summary>
    public BatteryPowerStatus Status { get; set; }

    /// <summary>Remaining capacity in milliwatt-hours.</summary>
    public int RemainingCapacityMWh { get; set; }
}
