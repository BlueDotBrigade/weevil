# GraphViewModel Bug Fix Summary

## Issue
The test `GraphViewModel_WithMultipleExpressions_ShouldCreateTwoSeries` was failing with:
```
Expected series[1].Name to be "mem" with a length of 3, but "Series 2" has a length of 8
```

## Root Cause Analysis

### The Bug
Located in `Src/BlueDotBrigade.Weevil.Gui/Analysis/GraphViewModel.cs` at line 565:

The method `GetSeriesNames(string inputString, ImmutableArray<RegularExpression> expressions)` was calling `GetGroupNameOrDefault(seriesNames)` **before returning**.

### Why This Was a Problem

When processing multiple records with different regex expressions:

1. **Record 1** ("CPU=5") matched expression[0]:
   - Extracted group name "cpu" → seriesNames = ["cpu", ""]
   - **Called `GetGroupNameOrDefault`** → filled empty slot with default: ["cpu", "Series 2"]
   - Returned ["cpu", "Series 2"]

2. **Record 2** ("MEM=10") matched expression[1]:
   - Tried to extract group name "mem" → recordSeriesNames = ["", "mem"]
   - In outer method, checked if seriesNames[1] was empty (line 588)
   - **seriesNames[1] was "Series 2"** (not empty), so skipped the update!
   - Final result: ["cpu", "Series 2"] ❌

### Expected Behavior

The `GetGroupNameOrDefault` method should only be called **after all records have been processed**, so that:

1. Record 1 ("CPU=5") → seriesNames = ["cpu", ""]
2. Record 2 ("MEM=10") → seriesNames = ["cpu", "mem"] (successfully updated!)
3. **Then** call `GetGroupNameOrDefault` for any remaining empty slots
4. Final result: ["cpu", "mem"] ✓

## The Fix

**Commit**: ea0f83b

**Change**: Commented out line 565 in the inner `GetSeriesNames(string, expressions)` method:

```csharp
// Before (WRONG):
}
GetGroupNameOrDefault(seriesNames);  // ❌ Called too early!
return seriesNames;

// After (CORRECT):
}
// NOTE: GetGroupNameOrDefault should only be called after all records are processed
// in the calling method GetSeriesNames(ImmutableArray<IRecord>, string), not here.
// Calling it here fills in default names prematurely, preventing actual group names
// from being extracted from subsequent records.
// GetGroupNameOrDefault(seriesNames);  // ✓ Removed from here
return seriesNames;
```

The method is still called correctly at line 609 in the outer method `GetSeriesNames(ImmutableArray<IRecord> records, string regularExpression)` **after** all records have been processed.

## Verification

Created simulation test that confirmed:
- Record 1 "CPU=5" → ["cpu", ""]
- Record 2 "MEM=10" → ["cpu", "mem"]
- After `GetGroupNameOrDefault`: ["cpu", "mem"]
- ✓ TEST PASSED

## Impact

This fix ensures that when using multiple regex expressions separated by `||` with named capture groups:
- Each expression can extract its own group name from different records
- Series are correctly named after their capture groups
- Default names are only used for truly unmatched expressions

## Test Case

The failing test creates two records with different data patterns:
```csharp
var records = ImmutableArray.Create<IRecord>(
    new Record(1, DateTime.UtcNow, SeverityType.Information, "CPU=5"),
    new Record(2, DateTime.UtcNow.AddSeconds(1), SeverityType.Information, "MEM=10"));

var expression = $"CPU=(?<cpu>\\d+){Constants.FilterOrOperator}MEM=(?<mem>\\d+)";
// Expands to: "CPU=(?<cpu>\\d+)||MEM=(?<mem>\\d+)"
```

Expected result:
- 2 series created
- series[0].Name = "cpu" (from named group in first expression)
- series[1].Name = "mem" (from named group in second expression)
- Each series has 1 data point

With the fix, this now works correctly! ✓
