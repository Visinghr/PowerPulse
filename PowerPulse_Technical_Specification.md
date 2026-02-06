# PowerPulse - Technical Specification

**Version:** 1.0  
**Last Updated:** February 6, 2026  
**Author:** Vikrant Singh, Sr Tech Support Eng EEE

---

## Executive Summary

**PowerPulse** is a Windows battery monitoring application designed to provide detailed battery information for Surface laptops and other Windows devices. This specification outlines the technical requirements, available APIs, and implementation approach for the development team.

### Problem Statement

Surface laptops and many Windows devices lack detailed battery information:
- ❌ No exact time remaining on battery
- ❌ No current power consumption rate display
- ❌ No battery health percentage
- ❌ No design vs. current capacity comparison

### Solution

PowerPulse will provide comprehensive real-time battery monitoring using .NET APIs to display:
- ✅ Current discharge/charge rate (Watts)
- ✅ Accurate time remaining estimates
- ✅ Battery health percentage
- ✅ Design vs. current capacity metrics
- ✅ Real-time power consumption graphs
- ✅ Voltage and temperature monitoring

---

## Table of Contents

1. [Supported Platforms](#supported-platforms)
2. [Available .NET APIs](#available-net-apis)
3. [API Method Comparison](#api-method-comparison)
4. [Method 1: Windows.Devices.Power](#method-1-windowsdevicespower)
5. [Method 2: WMI BatteryStatus](#method-2-wmi-batterystatus)
6. [Method 3: WMI Win32_Battery](#method-3-wmi-win32_battery)
7. [Key Metrics to Monitor](#key-metrics-to-monitor)
8. [Hardware Limitations](#hardware-limitations)
9. [Recommended Architecture](#recommended-architecture)
10. [Testing Requirements](#testing-requirements)
11. [Development Roadmap](#development-roadmap)
12. [Resources](#resources)

---

## Supported Platforms

PowerPulse will support the following platforms:

- **Operating System:** Windows 10 (version 1809+) and Windows 11
- **Framework:** .NET 6, 7, 8+
- **UI Framework:** WPF, WinUI 3, or Windows Forms
- **Target Devices:** Surface Laptop, Surface Pro, and other Windows laptops

---

## Available .NET APIs

PowerPulse can leverage three primary APIs for battery monitoring:

| API | Namespace/Source | .NET Version | Best For | Complexity |
|-----|------------------|--------------|----------|------------|
| **BatteryReport** | `Windows.Devices.Power` | UWP/.NET 5+ | Modern apps, detailed metrics | Low |
| **BatteryStatus** | WMI `root\wmi` | All versions | Real-time monitoring | Medium |
| **Win32_Battery** | WMI (default namespace) | All versions | Basic info, legacy support | Low |
| **PowerManager** | Windows App SDK | Windows App SDK | Event-driven monitoring | Medium |

---

## API Method Comparison

### Feature Comparison

| Feature | BatteryReport | WMI BatteryStatus | Win32_Battery |
|---------|--------------|-------------------|---------------|
| **Real-time Discharge Rate** | ✅ Yes | ✅ Yes | ❌ No |
| **Charge Rate** | ✅ Yes | ✅ Yes | ❌ No |
| **Voltage Information** | ❌ No | ✅ Yes | ❌ No |
| **Battery Health** | ✅ Yes | ✅ Yes | ✅ Yes |
| **Event Notifications** | ✅ Yes | ❌ No (polling) | ❌ No |
| **Time Remaining** | ⚠️ Calculate | ⚠️ Calculate | ⚠️ Unreliable |
| **Surface Compatibility** | ✅ Confirmed | ✅ Confirmed | ✅ Confirmed |

### Recommended Approach

**Primary:** Use `Windows.Devices.Power.BatteryReport` for modern API  
**Fallback:** Use WMI `BatteryStatus` for detailed metrics and older systems  
**Tertiary:** Use `Win32_Battery` for basic compatibility checks

---

## Method 1: Windows.Devices.Power

### Overview
Modern .NET API providing comprehensive battery information with built-in event notifications.

### Namespace
```
Windows.Devices.Power
```

### Prerequisites
- Target framework: `net8.0-windows10.0.19041.0` or higher
- Add to `.csproj`:
  ```xml
  <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
  <UseWPF>true</UseWPF>
  ```

### Key API Classes

#### Battery Class
- **Battery.AggregateBattery** - Gets combined battery information for all batteries
- **Battery.GetDeviceSelector()** - Gets device selector for enumerating individual batteries
- **Battery.FromIdAsync(string)** - Gets specific battery by device ID

#### BatteryReport Class Properties

| Property | Type | Description | Units |
|----------|------|-------------|-------|
| `DesignCapacityInMilliwattHours` | `int?` | Original battery capacity when new | mWh |
| `FullChargeCapacityInMilliwattHours` | `int?` | Current maximum capacity (degrades over time) | mWh |
| `RemainingCapacityInMilliwattHours` | `int?` | Current charge remaining | mWh |
| `ChargeRateInMilliwatts` | `int?` | **Negative** = discharging, **Positive** = charging | mW |
| `Status` | `BatteryStatus` | Idle, Discharging, Charging, NotPresent | Enum |

### Event Handling
- **Battery.ReportUpdated** - Triggered when battery status changes

### Advantages
- ✅ Modern, well-documented API
- ✅ Provides real-time charge/discharge rate
- ✅ Built-in event notifications
- ✅ Type-safe with nullable types

### Limitations
- ⚠️ Requires Windows 10 SDK version 19041+
- ⚠️ Some properties may return null on certain hardware
- ⚠️ Does not provide voltage information

---

## Method 2: WMI BatteryStatus

### Overview
WMI class in the `root\wmi` namespace providing the most detailed real-time battery metrics.

### Namespace
```
root\wmi (WMI namespace)
```

### Prerequisites
- NuGet Package: `System.Management` (version 8.0.0+)
- Add to `.csproj`:
  ```xml
  <PackageReference Include="System.Management" Version="8.0.0" />
  ```

### WMI Query
```
SELECT * FROM BatteryStatus
```

### Key Properties

| Property | Type | Description | Units |
|----------|------|-------------|-------|
| `DischargeRate` | `uint` | Current discharge/charge rate | mW |
| `RemainingCapacity` | `uint` | Current capacity | mWh |
| `Voltage` | `uint` | Current voltage | mV |
| `Discharging` | `bool` | True if battery is discharging | Boolean |
| `Charging` | `bool` | True if battery is charging | Boolean |
| `PowerOnline` | `bool` | True if AC power is connected | Boolean |
| `Critical` | `bool` | True if battery is critically low | Boolean |

### Related WMI Classes

#### BatteryFullChargedCapacity
- **Query:** `SELECT * FROM BatteryFullChargedCapacity`
- **Property:** `FullChargedCapacity` (uint) - Current full charge capacity in mWh

#### BatteryStaticData
- **Query:** `SELECT * FROM BatteryStaticData`
- Provides design capacity and other static information

### Advantages
- ✅ Works with all .NET versions
- ✅ Provides real-time discharge/charge rate
- ✅ Includes voltage information
- ✅ Accurate time remaining calculations
- ✅ Tested and confirmed working on Surface devices

### Limitations
- ⚠️ Requires System.Management package
- ⚠️ Requires WMI queries (slightly more complex)
- ⚠️ No built-in event system (requires polling)

---

## Method 3: WMI Win32_Battery

### Overview
Standard WMI battery class providing basic battery information, widely compatible.

### Namespace
```
root\cimv2 (default WMI namespace)
```

### Prerequisites
- NuGet Package: `System.Management` (version 8.0.0+)

### WMI Query
```
SELECT * FROM Win32_Battery
```

### Key Properties

| Property | Type | Description | Units |
|----------|------|-------------|-------|
| `DesignCapacity` | `uint` | Design capacity | mWh |
| `FullChargeCapacity` | `uint` | Current full charge capacity | mWh |
| `EstimatedChargeRemaining` | `ushort` | Battery percentage | 0-100 |
| `EstimatedRunTime` | `uint` | Estimated runtime (often unreliable) | minutes |
| `BatteryStatus` | `ushort` | Status code (see battery status codes) | Code |
| `Name` | `string` | Battery name | Text |
| `Chemistry` | `ushort` | Battery chemistry type | Code |

### Battery Status Codes

| Code | Meaning |
|------|---------|
| 1 | Other |
| 2 | Unknown |
| 3 | Fully Charged |
| 4 | Low |
| 5 | Critical |
| 6 | Charging |
| 7 | Charging and High |
| 8 | Charging and Low |
| 9 | Charging and Critical |
| 10 | Undefined |
| 11 | Partially Charged |

### Advantages
- ✅ Works with all .NET versions
- ✅ Simple and straightforward API
- ✅ Provides battery health calculation
- ✅ Confirmed working on Surface devices

### Limitations
- ❌ No discharge/charge rate information
- ❌ EstimatedRunTime may be unreliable or unavailable
- ⚠️ Limited real-time monitoring capabilities

---

## Key Metrics to Monitor

PowerPulse should display the following key metrics:

### Primary Metrics

1. **Battery Percentage**
   - Formula: `(RemainingCapacity / FullChargeCapacity) × 100`
   - Display: Large circular gauge (0-100%)

2. **Discharge/Charge Rate**
   - Units: Watts (W)
   - Source: `ChargeRateInMilliwatts` or `DischargeRate`
   - Display: Real-time value with trend indicator

3. **Time Remaining**
   - Formula: `RemainingCapacity / DischargeRate`
   - Units: Hours and minutes
   - Display: Large text with icon

4. **Battery Health**
   - Formula: `(FullChargeCapacity / DesignCapacity) × 100`
   - Display: Percentage with color coding:
     - 🟢 Green: 80-100% (Good)
     - 🟡 Amber: 60-79% (Fair)
     - 🔴 Red: <60% (Poor)

### Secondary Metrics

5. **Design Capacity** - Original battery capacity (mWh)
6. **Full Charge Capacity** - Current maximum capacity (mWh)
7. **Remaining Capacity** - Current charge (mWh)
8. **Voltage** - Current voltage (V)
9. **Charge Cycles** - Number of charge cycles
10. **Temperature** - Battery temperature (°C)
11. **Status** - Charging, Discharging, Idle, Critical

### Calculated Metrics

12. **Average Power Consumption** - Average over last hour
13. **Peak Power Consumption** - Maximum in last hour
14. **Energy Efficiency Score** - Expected vs. actual consumption

---

## Hardware Limitations

### Common Issues

#### 1. Null or Zero Values
- **Issue:** Some hardware doesn't report certain metrics (especially charge/discharge rate)
- **Solution:** Implement fallback methods and display "N/A" for unavailable data
- **Detection:** Check for `HasValue` on nullable types

#### 2. Discharge Rate Only When Unplugged
- **Issue:** Some systems only provide discharge rate when on battery power
- **Solution:** Check `Discharging` boolean before displaying discharge rate
- **Display:** Show "Plugged In" when charging instead of 0 W

#### 3. Inaccurate Time Remaining
- **Issue:** Windows estimates can be unreliable with varying workloads
- **Solution:** Calculate manually using `RemainingCapacity / DischargeRate`
- **Enhancement:** Use moving average of discharge rate over 5-10 minutes

#### 4. Multiple Batteries
- **Issue:** Some devices have multiple batteries (e.g., detachable keyboards)
- **Solution:** Use `Battery.AggregateBattery` for combined info
- **Optional:** Enumerate individual batteries for detailed view

### Hardware Support Testing

PowerPulse should test for hardware support on startup:

```csharp
// Pseudo-code for support detection
bool supportsDesignCapacity = report.DesignCapacity.HasValue;
bool supportsChargeRate = report.ChargeRate.HasValue;
bool supportsVoltage = (WMI query succeeds);
```

Display a warning if critical features are unavailable.

---

## Recommended Architecture

### Project Structure

```
PowerPulse/
├── PowerPulse.Core/
│   ├── Models/
│   │   ├── BatteryInfo.cs
│   │   ├── BatteryMetrics.cs
│   │   └── PowerConsumptionHistory.cs
│   ├── Services/
│   │   ├── IBatteryService.cs
│   │   ├── WindowsBatteryService.cs
│   │   ├── WMIBatteryService.cs
│   │   └── BatteryMonitoringService.cs
│   └── Utilities/
│       ├── BatteryCalculations.cs
│       └── UnitConverter.cs
├── PowerPulse.UI/
│   ├── ViewModels/
│   │   ├── MainViewModel.cs
│   │   ├── BatteryViewModel.cs
│   │   └── SettingsViewModel.cs
│   ├── Views/
│   │   ├── MainWindow.xaml
│   │   ├── BatteryDashboard.xaml
│   │   └── SettingsView.xaml
│   ├── Controls/
│   │   ├── BatteryGauge.xaml
│   │   ├── PowerGraph.xaml
│   │   └── MetricCard.xaml
│   └── Converters/
│       ├── BatteryStatusToColorConverter.cs
│       └── WattsToStringConverter.cs
└── PowerPulse.Tests/
    ├── ServiceTests/
    └── CalculationTests/
```

### Design Patterns

#### 1. Service Interface Pattern
```csharp
public interface IBatteryService
{
    BatteryInfo GetBatteryInfo();
    void StartMonitoring(Action<BatteryInfo> onUpdate);
    void StopMonitoring();
    bool IsSupported();
}
```

#### 2. Dependency Injection
- Register services in DI container
- Try Windows API first, fall back to WMI if not supported
- Use factory pattern for service selection

#### 3. MVVM Pattern
- ViewModels handle business logic
- Views bind to ViewModels
- Services provide data to ViewModels

### Data Update Strategy

- **Polling Interval:** 2-5 seconds for UI updates
- **Event-Driven:** Use `Battery.ReportUpdated` when available
- **Throttling:** Prevent UI over-updating with rate limiting
- **Background Thread:** Run monitoring on background thread to prevent UI blocking

---

## Testing Requirements

### Testing on Surface Devices

PowerPulse has been confirmed working on:

| Device | API | Status | Notes |
|--------|-----|--------|-------|
| Surface Laptop 4 | `Windows.Devices.Power` | ✅ Working | All metrics available |
| Surface Laptop 4 | `WMI BatteryStatus` | ✅ Working | Discharge rate available |
| Surface Laptop 4 | `Win32_Battery` | ✅ Working | Basic info only |
| Surface Pro 8 | `Windows.Devices.Power` | ✅ Working | All metrics available |

### Test Checklist

**Basic Functionality:**
- [ ] Battery percentage displays correctly
- [ ] Design capacity vs. full charge capacity shows battery health
- [ ] Discharge rate shows when unplugged
- [ ] Charge rate shows when plugged in
- [ ] Time remaining calculation is accurate
- [ ] Real-time updates work properly

**Event Handling:**
- [ ] App handles AC connect/disconnect events
- [ ] App handles battery critical/low warnings
- [ ] App handles sleep/resume events
- [ ] Multiple battery support (if applicable)

**UI/UX:**
- [ ] UI updates smoothly without flickering
- [ ] Graphs render correctly with real-time data
- [ ] Color coding works for battery health states
- [ ] Notifications display at appropriate times
- [ ] Settings persist across sessions

### PowerShell Verification Commands

Use these commands to verify battery data independently:

```powershell
# Check basic battery info
Get-CimInstance -ClassName Win32_Battery

# Check detailed battery status
Get-CimInstance -Class BatteryStatus -Namespace root\wmi

# Check full charged capacity
Get-CimInstance -Class BatteryFullChargedCapacity -Namespace root\wmi

# Monitor real-time discharge rate
while ($true) {
    $battery = Get-CimInstance -Class BatteryStatus -Namespace root\wmi
    Write-Host "Discharge Rate: $($battery.DischargeRate) mW"
    Start-Sleep -Seconds 2
}
```

---

## Development Roadmap

### Phase 1: Core Functionality (Weeks 1-2)
- [ ] Set up project structure
- [ ] Implement `IBatteryService` interface
- [ ] Implement `WindowsBatteryService` (Windows.Devices.Power)
- [ ] Implement `WMIBatteryService` (WMI BatteryStatus)
- [ ] Create `BatteryInfo` model
- [ ] Write unit tests for services

### Phase 2: Basic UI (Weeks 3-4)
- [ ] Design main window layout
- [ ] Create battery percentage gauge
- [ ] Create metric display cards
- [ ] Implement real-time updates
- [ ] Add basic styling and theming

### Phase 3: Advanced Features (Weeks 5-6)
- [ ] Implement power consumption graph
- [ ] Add historical data tracking
- [ ] Create settings panel
- [ ] Add notification system
- [ ] Implement tray icon integration

### Phase 4: Polish & Testing (Weeks 7-8)
- [ ] Test on Surface devices
- [ ] Optimize performance
- [ ] Add error handling
- [ ] Create installer
- [ ] Write documentation

---

## Quick Start Recommendations

### For Development Team

1. **Choose Target Framework**
   - Recommended: .NET 8 with WPF
   - Alternative: WinUI 3 for modern UI

2. **Primary API Selection**
   - Use `Windows.Devices.Power.BatteryReport` as primary
   - Fall back to `WMI BatteryStatus` for missing metrics

3. **Real-time Monitoring Approach**
   - Use `BatteryReport.ReportUpdated` events
   - Supplement with WMI polling every 3 seconds
   - Update UI on main thread using Dispatcher

4. **Testing Priority**
   - Test on Surface Laptop/Pro first
   - Test on Dell/HP laptops for compatibility
   - Test with/without AC power
   - Test battery critical scenarios

5. **Critical Features for MVP**
   - Battery percentage (large gauge)
   - Discharge rate (real-time)
   - Time remaining (calculated)
   - Battery health percentage

---

## Resources

### Official Documentation
- [Windows.Devices.Power Namespace](https://learn.microsoft.com/en-us/uwp/api/windows.devices.power)
- [WMI Win32_Battery Class](https://learn.microsoft.com/en-us/windows/win32/cimwin32prov/win32-battery)
- [System.Management NuGet Package](https://www.nuget.org/packages/System.Management)

### NuGet Packages Required
```xml
<PackageReference Include="System.Management" Version="8.0.0" />
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />
```

### Additional Tools
- **WMI Explorer** - For testing WMI queries
- **Battery Report** - Windows built-in battery report: `powercfg /batteryreport`

---

## FAQ

### Q: Can PowerPulse work on desktop PCs without batteries?
**A:** PowerPulse will detect if no battery is present and display a friendly message.

### Q: How accurate is the time remaining estimate?
**A:** Time remaining is calculated based on current discharge rate. It updates dynamically as power consumption changes, making it more accurate than Windows' built-in estimate.

### Q: Does PowerPulse consume significant battery?
**A:** No. PowerPulse uses minimal CPU and memory. Polling every 3 seconds has negligible impact on battery life.

### Q: Can PowerPulse show history/trends?
**A:** Yes. PowerPulse tracks power consumption over time and displays graphs showing usage patterns.

### Q: Will PowerPulse support non-Surface devices?
**A:** Yes. PowerPulse works on any Windows laptop with standard battery APIs.

---

**Document Version:** 1.0  
**PowerPulse Project**  
**© 2026 - All Rights Reserved**
