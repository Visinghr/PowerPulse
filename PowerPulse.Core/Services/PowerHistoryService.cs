using PowerPulse.Core.Models;

namespace PowerPulse.Core.Services;

/// <summary>
/// Manages historical power consumption data for graphing and trend analysis.
/// Keeps samples in memory for the last hour (max 1200 samples at 3s intervals).
/// </summary>
public class PowerHistoryService
{
    private readonly List<PowerSample> _samples = new();
    private readonly TimeSpan _retentionPeriod = TimeSpan.FromHours(1);
    private readonly int _maxSamples = 1200; // 1 hour at 3s intervals
    private readonly object _lock = new();

    /// <summary>
    /// Adds a new power sample to the history.
    /// Automatically prunes old samples beyond the retention period.
    /// </summary>
    public void AddSample(BatteryInfo info)
    {
        lock (_lock)
        {
            var sample = new PowerSample
            {
                Timestamp = info.Timestamp,
                Percentage = info.Percentage,
                DischargeRateMW = info.DischargeRateMW,
                ChargeRateMW = info.ChargeRateMW,
                Status = info.Status,
                RemainingCapacityMWh = info.RemainingCapacityMWh
            };

            _samples.Add(sample);

            // Prune old samples
            var cutoffTime = DateTime.Now - _retentionPeriod;
            _samples.RemoveAll(s => s.Timestamp < cutoffTime);

            // Also enforce max sample count as a safety limit
            if (_samples.Count > _maxSamples)
            {
                _samples.RemoveRange(0, _samples.Count - _maxSamples);
            }
        }
    }

    /// <summary>
    /// Gets all samples from the last specified duration.
    /// </summary>
    public List<PowerSample> GetSamples(TimeSpan duration)
    {
        lock (_lock)
        {
            var cutoffTime = DateTime.Now - duration;
            return _samples.Where(s => s.Timestamp >= cutoffTime).ToList();
        }
    }

    /// <summary>
    /// Gets all samples (up to 1 hour).
    /// </summary>
    public List<PowerSample> GetAllSamples()
    {
        lock (_lock)
        {
            return new List<PowerSample>(_samples);
        }
    }

    /// <summary>
    /// Calculates average discharge rate over the specified duration.
    /// </summary>
    public double GetAverageDischargeRate(TimeSpan duration)
    {
        var samples = GetSamples(duration);
        var dischargeSamples = samples.Where(s => s.DischargeRateMW > 0).ToList();
        
        return dischargeSamples.Any() 
            ? dischargeSamples.Average(s => s.DischargeRateMW) 
            : 0;
    }

    /// <summary>
    /// Gets the peak discharge rate over the specified duration.
    /// </summary>
    public int GetPeakDischargeRate(TimeSpan duration)
    {
        var samples = GetSamples(duration);
        var dischargeSamples = samples.Where(s => s.DischargeRateMW > 0);
        
        return dischargeSamples.Any() 
            ? dischargeSamples.Max(s => s.DischargeRateMW) 
            : 0;
    }

    /// <summary>
    /// Clears all historical data.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _samples.Clear();
        }
    }

    /// <summary>
    /// Gets the number of samples currently stored.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _samples.Count;
            }
        }
    }
}
