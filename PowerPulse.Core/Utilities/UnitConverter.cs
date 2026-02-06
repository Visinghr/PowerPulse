namespace PowerPulse.Core.Utilities;

/// <summary>
/// Unit conversion helpers for battery metrics.
/// </summary>
public static class UnitConverter
{
    /// <summary>Convert milliwatts to watts.</summary>
    public static double MilliwattsToWatts(int mw) => Math.Round(mw / 1000.0, 2);

    /// <summary>Convert milliwatt-hours to watt-hours.</summary>
    public static double MilliwattHoursToWattHours(int mwh) => Math.Round(mwh / 1000.0, 2);

    /// <summary>Convert millivolts to volts.</summary>
    public static double MillivoltsToVolts(int mv) => Math.Round(mv / 1000.0, 2);

    /// <summary>Format a TimeSpan as "Xh Ym" for display.</summary>
    public static string FormatTimeRemaining(TimeSpan? time)
    {
        if (time == null || time.Value.TotalMinutes <= 0)
            return "Calculating...";

        var ts = time.Value;
        if (ts.TotalHours >= 24)
            return "24h+";

        return ts.Hours > 0
            ? $"{ts.Hours}h {ts.Minutes}m"
            : $"{ts.Minutes}m";
    }

    /// <summary>Format watts for display (e.g., "12.4 W").</summary>
    public static string FormatWatts(double watts)
    {
        return watts < 0.1 ? "0 W" : $"{watts:F1} W";
    }
}
