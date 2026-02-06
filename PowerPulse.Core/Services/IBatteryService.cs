using PowerPulse.Core.Models;

namespace PowerPulse.Core.Services;

/// <summary>
/// Interface for battery data providers.
/// Implementations read battery metrics from different Windows APIs.
/// </summary>
public interface IBatteryService
{
    /// <summary>Gets current battery information.</summary>
    BatteryInfo GetBatteryInfo();

    /// <summary>Starts periodic monitoring, calling onUpdate on each tick.</summary>
    void StartMonitoring(Action<BatteryInfo> onUpdate, int intervalMs = 3000);

    /// <summary>Stops periodic monitoring.</summary>
    void StopMonitoring();

    /// <summary>Returns true if this battery API is available on the current hardware.</summary>
    bool IsSupported();
}
