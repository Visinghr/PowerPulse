# Battery Time Estimation Features

## Overview

PowerPulse now provides **three different battery time remaining estimates** to give users comprehensive information about their battery life:

1. **Adaptive EMA (Historical)** - The primary estimate based on historical discharge patterns
2. **Real-Time (Last Minute)** - An immediate estimate based on the last 60 seconds of usage
3. **Pattern-Based (Experimental)** - A learned estimate based on typical usage patterns for the current hour of the day

## Implementation Details

### 1. Adaptive EMA (Exponential Moving Average)

**Purpose**: Provides smooth, stable estimates that don't fluctuate wildly with brief CPU spikes.

**Algorithm**:
```
smoothedRate = α × currentRate + (1 - α) × previousSmoothedRate
timeRemaining = remainingCapacity / smoothedRate
```

**Features**:
- **Adaptive α**: Uses higher α (0.25) when rate changes significantly (>50%) for quick adaptation to genuine workload shifts
- Uses lower α (0.05) during stable periods for smooth estimates
- Caps estimates at 24 hours maximum
- Minimum discharge rate threshold of 100mW to avoid unrealistic estimates

**Location**: `BatteryCalculations.UpdateAndEstimate()`

### 2. Real-Time Estimate (Last Minute Average)

**Purpose**: Provides an immediate estimate based on very recent usage, especially useful when the UI is open.

**How it works**:
- Tracks the last **20 discharge rate readings** (60 seconds at 3-second polling intervals)
- Calculates a simple **average** of these readings
- More responsive to current load than the EMA approach

**Data Structure**: `Queue<int>` with max 20 items

**Location**: `BatteryCalculations.GetRealTimeEstimate()`

**Use Case**: When you want to see how current activity (e.g., watching a video, compiling code) affects battery life right now.

### 3. Pattern-Based Estimate (Experimental)

**Purpose**: Learns typical usage patterns throughout the day to provide more realistic estimates.

**How it works**:
- Tracks discharge rates grouped by **hour of the day** (0-23)
- Stores up to **100 readings per hour** to prevent unbounded memory growth
- Requires at least **10 readings** for the current hour before providing an estimate
- Uses **median** (not average) discharge rate for robustness against outliers

**Data Structure**: `Dictionary<int, List<int>>` (hour → list of discharge rates)

**Data Persistence**: Hourly patterns are preserved across battery sessions (not cleared on AC power) to continue learning over time

**Location**: `BatteryCalculations.GetPatternBasedEstimate()`

**Use Case**: If you typically use your laptop for light work in the morning and heavy work in the afternoon, the pattern-based estimate will reflect this by showing different estimates based on the time of day.

## UI Display

The UI shows all three estimates in a hierarchical layout:

```
┌─────────────────────────────────────┐
│ Time Remaining (Adaptive EMA)       │
│ ⏱ 4h 23m                (Large)     │
│                                      │
│ ┌────────────────┬─────────────────┐│
│ │ Real-Time      │ Pattern-Based   ││
│ │ (Last Min)     │ (Exp.)          ││
│ │ 4h 15m         │ 4h 30m          ││
│ └────────────────┴─────────────────┘│
└─────────────────────────────────────┘
```

- **Primary**: Adaptive EMA is displayed prominently at the top (24pt font)
- **Secondary**: Real-Time and Pattern-Based estimates are shown below in smaller cards (16pt font)

## Data Flow

```
BatteryMonitoringService (every 3 seconds)
  ↓
BatteryCalculations.UpdateAndEstimate()
  ├─ Tracks recent rates (Queue)
  ├─ Tracks hourly patterns (Dictionary)
  ├─ Updates EMA calculation
  └─ Returns EMA estimate
  
When discharging:
  BatteryCalculations.GetRealTimeEstimate()
    └─ Calculates average of last 20 readings
    
  BatteryCalculations.GetPatternBasedEstimate()
    └─ Calculates median for current hour
    
  ↓
BatteryInfo object
  ├─ EstimatedTimeRemaining (EMA)
  ├─ RealTimeEstimate
  └─ PatternBasedEstimate
  
  ↓
MainViewModel updates UI properties
  ├─ TimeRemainingText
  ├─ RealTimeEstimateText
  └─ PatternEstimateText
```

## Reset Behavior

When AC power is connected:

- **EMA state** is reset (smoothed rate cleared)
- **Recent rates queue** is cleared (real-time estimate resets)
- **Hourly patterns** are **preserved** to continue learning across sessions

This design ensures that:
1. Fresh battery sessions start with clean short-term data
2. Long-term learning (patterns) is retained for better predictions

## Memory Management

- **Recent rates**: Fixed size queue (max 20 items)
- **Hourly patterns**: Each hour keeps max 100 readings
- **Total memory**: Up to 24 hours × 100 readings = 2,400 integers (~9.6 KB)

This bounded approach prevents unbounded memory growth while maintaining sufficient data for accurate pattern recognition.

## Future Enhancements

Potential improvements to consider:

1. **Persistence**: Save hourly patterns to disk for cross-session learning
2. **Day-of-week patterns**: Different usage on weekdays vs. weekends
3. **Activity detection**: Correlate patterns with specific applications or tasks
4. **Confidence indicators**: Show how confident each estimate is based on data quality
5. **User feedback**: Allow users to mark which estimate was most accurate
6. **Hybrid approach**: Combine all three estimates using weighted average based on confidence

## Technical Notes

- All calculations use milliwatts (mW) and milliwatt-hours (mWh) for precision
- Minimum threshold of 100mW prevents division by near-zero values
- All estimates are capped at 24 hours maximum
- Polling interval is 3 seconds (as per specification)
- Pattern-based estimate may show "Learning..." until sufficient data is collected

## Files Modified

1. `PowerPulse.Core/Utilities/BatteryCalculations.cs` - Core calculation logic
2. `PowerPulse.Core/Models/BatteryInfo.cs` - Added new estimate properties
3. `PowerPulse.Core/Services/BatteryMonitoringService.cs` - Calculate all estimates
4. `PowerPulse.UI/ViewModels/MainViewModel.cs` - Added UI properties
5. `PowerPulse.UI/Views/MainWindow.xaml` - Updated UI layout
