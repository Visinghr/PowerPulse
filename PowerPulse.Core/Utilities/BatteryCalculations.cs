namespace PowerPulse.Core.Utilities;

/// <summary>
/// Calculates estimated battery time remaining using multiple approaches:
/// 1. Exponential Moving Average (EMA) for historical data-based calculation
/// 2. Real-time calculation based on the last minute of average usage
/// 3. Experimental pattern analysis for more realistic estimates
///
/// EMA Algorithm:
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

    // Real-time calculation: track last minute of discharge rates (20 readings at 3s intervals)
    private readonly Queue<int> _recentRates = new();
    private const int MaxRecentRates = 20; // 20 readings × 3 seconds = 60 seconds

    // Pattern analysis: track hourly usage patterns (experimental)
    private readonly Dictionary<int, List<int>> _hourlyPatterns = new(); // Hour -> list of discharge rates

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

        // Track recent rates for real-time calculation
        _recentRates.Enqueue(currentRateMW);
        if (_recentRates.Count > MaxRecentRates)
            _recentRates.Dequeue();

        // Track hourly patterns for experimental analysis
        int currentHour = DateTime.Now.Hour;
        if (!_hourlyPatterns.ContainsKey(currentHour))
            _hourlyPatterns[currentHour] = new List<int>();
        _hourlyPatterns[currentHour].Add(currentRateMW);
        
        // Keep only last 100 readings per hour to prevent unbounded growth
        if (_hourlyPatterns[currentHour].Count > 100)
            _hourlyPatterns[currentHour].RemoveAt(0);

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
    /// Calculates real-time estimated time remaining based on the average of the last minute of usage.
    /// This provides a more immediate estimate when the UI is open.
    /// </summary>
    /// <param name="remainingCapacityMWh">Remaining battery capacity in milliwatt-hours.</param>
    /// <returns>Real-time estimated time remaining, or null if not enough data.</returns>
    public TimeSpan? GetRealTimeEstimate(int remainingCapacityMWh)
    {
        if (_recentRates.Count == 0 || remainingCapacityMWh <= 0)
            return null;

        // Calculate average discharge rate from recent readings
        double avgRateMW = _recentRates.Average();

        // Avoid unrealistic estimates from tiny discharge rates
        if (avgRateMW < MinRateMW)
            return null;

        // Time = Capacity / Rate
        double hoursRemaining = remainingCapacityMWh / avgRateMW;

        // Cap at 24 hours
        if (hoursRemaining > 24.0)
            hoursRemaining = 24.0;

        return TimeSpan.FromHours(hoursRemaining);
    }

    /// <summary>
    /// Experimental: Calculates estimated time remaining based on historical usage patterns for the current hour.
    /// This provides a more realistic estimate by learning typical usage at different times of day.
    /// </summary>
    /// <param name="remainingCapacityMWh">Remaining battery capacity in milliwatt-hours.</param>
    /// <returns>Pattern-based estimated time remaining, or null if not enough data.</returns>
    public TimeSpan? GetPatternBasedEstimate(int remainingCapacityMWh)
    {
        if (remainingCapacityMWh <= 0)
            return null;

        int currentHour = DateTime.Now.Hour;
        
        // Need at least 10 readings for this hour to make a pattern-based estimate
        if (!_hourlyPatterns.ContainsKey(currentHour) || _hourlyPatterns[currentHour].Count < 10)
            return null;

        // Calculate median discharge rate for this hour (median is more robust than average)
        var sortedRates = _hourlyPatterns[currentHour].OrderBy(r => r).ToList();
        double medianRate = sortedRates[sortedRates.Count / 2];

        // Avoid unrealistic estimates
        if (medianRate < MinRateMW)
            return null;

        // Time = Capacity / Rate
        double hoursRemaining = remainingCapacityMWh / medianRate;

        // Cap at 24 hours
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
        _recentRates.Clear();
        // Note: We keep _hourlyPatterns to preserve learned patterns across sessions
    }
}
