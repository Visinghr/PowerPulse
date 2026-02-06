namespace PowerPulse.Core.Utilities;

/// <summary>
/// Calculates estimated battery time remaining using Exponential Moving Average (EMA)
/// of the discharge rate. This provides smooth, stable estimates that don't jump wildly
/// with brief CPU spikes.
///
/// Algorithm:
///   smoothedRate = α × currentRate + (1 - α) × previousSmoothedRate
///   timeRemaining = remainingCapacity / smoothedRate
///
/// Adaptive α: Uses higher α (0.25) when rate changes significantly to respond
/// quickly to genuine workload shifts; lower α (0.05) during stable periods.
/// </summary>
public class BatteryCalculations
{
    private double _smoothedRateMW;
    private bool _initialized;

    // EMA smoothing factors
    private const double AlphaStable = 0.05;    // Steady state: smooth, slow response
    private const double AlphaAdaptive = 0.25;   // Large change: quick adaptation
    private const double RateChangeThreshold = 0.50; // 50% change triggers fast adaptation

    // Minimum discharge rate to consider (avoids division by very small numbers)
    private const double MinRateMW = 100; // 0.1W minimum

    /// <summary>
    /// Updates the EMA-smoothed discharge rate and returns estimated time remaining.
    /// Call this on every polling tick (every 3 seconds).
    /// </summary>
    /// <param name="currentRateMW">Current discharge rate in milliwatts (positive = discharging).</param>
    /// <param name="remainingCapacityMWh">Remaining battery capacity in milliwatt-hours.</param>
    /// <returns>Estimated time remaining, or null if not enough data or not discharging.</returns>
    public TimeSpan? UpdateAndEstimate(int currentRateMW, int remainingCapacityMWh)
    {
        // Skip if not discharging or rate is invalid
        if (currentRateMW <= 0 || remainingCapacityMWh <= 0)
        {
            // Reset when on AC so next discharge starts fresh
            Reset();
            return null;
        }

        double rate = currentRateMW;

        if (!_initialized)
        {
            // First reading: seed the EMA with current value
            _smoothedRateMW = rate;
            _initialized = true;
        }
        else
        {
            // Choose alpha based on how much the rate changed
            double changeRatio = Math.Abs(rate - _smoothedRateMW) / _smoothedRateMW;
            double alpha = changeRatio > RateChangeThreshold ? AlphaAdaptive : AlphaStable;

            // EMA update
            _smoothedRateMW = alpha * rate + (1.0 - alpha) * _smoothedRateMW;
        }

        // Avoid unrealistic estimates from tiny discharge rates
        if (_smoothedRateMW < MinRateMW)
            return null;

        // Time = Capacity / Rate (both in mW units → result in hours)
        double hoursRemaining = remainingCapacityMWh / _smoothedRateMW;

        // Cap at 24 hours (anything beyond is likely a measurement error)
        if (hoursRemaining > 24.0)
            hoursRemaining = 24.0;

        return TimeSpan.FromHours(hoursRemaining);
    }

    /// <summary>
    /// Gets the current EMA-smoothed discharge rate in milliwatts.
    /// </summary>
    public double SmoothedRateMW => _smoothedRateMW;

    /// <summary>
    /// Resets the estimator state. Called when switching to AC power
    /// so the next battery session starts with fresh data.
    /// </summary>
    public void Reset()
    {
        _smoothedRateMW = 0;
        _initialized = false;
    }
}
