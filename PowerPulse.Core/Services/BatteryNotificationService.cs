using PowerPulse.Core.Models;

namespace PowerPulse.Core.Services;

/// <summary>
/// Manages battery-related notifications and alerts.
/// Tracks battery state changes and triggers appropriate notifications.
/// </summary>
public class BatteryNotificationService
{
    private bool _lowBatteryNotified;
    private bool _criticalBatteryNotified;
    private bool _fullyChargedNotified;
    private int _lastPercentage = -1;
    private BatteryPowerStatus _lastStatus = BatteryPowerStatus.Unknown;

    // Notification thresholds
    public int LowBatteryThreshold { get; set; } = 20; // 20%
    public int CriticalBatteryThreshold { get; set; } = 10; // 10%
    public bool NotifyOnFullyCharged { get; set; } = true;
    public bool NotifyOnLowBattery { get; set; } = true;
    public bool NotifyOnCriticalBattery { get; set; } = true;

    /// <summary>Event fired when a notification should be shown.</summary>
    public event EventHandler<BatteryNotificationEventArgs>? NotificationRequested;

    /// <summary>
    /// Checks battery state and fires notification events if needed.
    /// Call this on every battery update.
    /// </summary>
    public void CheckAndNotify(BatteryInfo info)
    {
        if (!info.IsBatteryPresent)
            return;

        // Check for critical battery (first priority)
        if (NotifyOnCriticalBattery && info.Percentage <= CriticalBatteryThreshold 
            && info.Status == BatteryPowerStatus.Discharging
            && !_criticalBatteryNotified)
        {
            _criticalBatteryNotified = true;
            _lowBatteryNotified = true; // Also mark low as notified
            RaiseNotification(
                NotificationType.Critical,
                "Critical Battery Level!",
                $"Only {info.Percentage}% remaining. Please plug in immediately.",
                NotificationPriority.High
            );
        }
        // Check for low battery
        else if (NotifyOnLowBattery && info.Percentage <= LowBatteryThreshold 
            && info.Percentage > CriticalBatteryThreshold
            && info.Status == BatteryPowerStatus.Discharging
            && !_lowBatteryNotified)
        {
            _lowBatteryNotified = true;
            RaiseNotification(
                NotificationType.LowBattery,
                "Low Battery",
                $"Battery is at {info.Percentage}%. Consider plugging in soon.",
                NotificationPriority.Medium
            );
        }

        // Check for fully charged
        if (NotifyOnFullyCharged && info.Percentage >= 100 
            && info.Status == BatteryPowerStatus.Idle
            && !_fullyChargedNotified)
        {
            _fullyChargedNotified = true;
            RaiseNotification(
                NotificationType.FullyCharged,
                "Battery Fully Charged",
                "Battery is at 100%. You can unplug the charger.",
                NotificationPriority.Low
            );
        }

        // Reset notification flags when conditions change
        if (info.Status == BatteryPowerStatus.Charging || info.Status == BatteryPowerStatus.Idle)
        {
            // Reset all battery warnings when plugged in
            _lowBatteryNotified = false;
            _criticalBatteryNotified = false;
        }

        if (info.Percentage < 100 || info.Status == BatteryPowerStatus.Discharging)
        {
            _fullyChargedNotified = false;
        }

        // Track state for next check
        _lastPercentage = info.Percentage;
        _lastStatus = info.Status;
    }

    /// <summary>
    /// Resets all notification flags. Call when user dismisses notifications.
    /// </summary>
    public void ResetNotifications()
    {
        _lowBatteryNotified = false;
        _criticalBatteryNotified = false;
        _fullyChargedNotified = false;
    }

    private void RaiseNotification(NotificationType type, string title, string message, NotificationPriority priority)
    {
        NotificationRequested?.Invoke(this, new BatteryNotificationEventArgs
        {
            Type = type,
            Title = title,
            Message = message,
            Priority = priority,
            Timestamp = DateTime.Now
        });
    }
}

/// <summary>
/// Event arguments for battery notifications.
/// </summary>
public class BatteryNotificationEventArgs : EventArgs
{
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationPriority Priority { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Types of battery notifications.
/// </summary>
public enum NotificationType
{
    LowBattery,
    Critical,
    FullyCharged,
    PluggedIn,
    Unplugged
}

/// <summary>
/// Notification priority levels.
/// </summary>
public enum NotificationPriority
{
    Low,
    Medium,
    High
}
