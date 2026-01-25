# GraphViewModel Test Analysis and Fix Documentation

## Test Under Investigation
`GraphViewModel_WithMultipleExpressions_ShouldCreateTwoSeries` in `Tst/BlueDotBrigade.Weevil.Gui-FeatureTests/Analysis/GraphViewModelTests.cs`

## Executive Summary
After comprehensive code analysis and simulation, **the implementation appears to be correct** and the test should pass. This document provides detailed analysis of the logic flow.

## Test Scenario
The test verifies that GraphViewModel can create two separate data series from a regex pattern containing multiple expressions separated by `||`:

```csharp
var records = ImmutableArray.Create<IRecord>(
    new Record(1, DateTime.UtcNow, SeverityType.Information, "CPU=5"),
    new Record(2, DateTime.UtcNow.AddSeconds(1), SeverityType.Information, "MEM=10"));

var expression = $"CPU=(?<cpu>\\d+){Constants.FilterOrOperator}MEM=(?<mem>\\d+)";
// Result: "CPU=(?<cpu>\\d+)||MEM=(?<mem>\\d+)"
```

**Expected Result:**
- 2 series created
- Series[0]: name="cpu", 1 data point (5)
- Series[1]: name="mem", 1 data point (10)

## Component Analysis

### 1. Constants.FilterOrOperator ✅
**Location:** `Src/BlueDotBrigade.Weevil.Common/Filter/Constants.cs`
**Value:** `"||"`
**Status:** Correctly defined and accessible

### 2. ParseRegularExpressions() ✅
**Location:** `GraphViewModel.cs` lines 388-416

**Logic:**
1. Splits input string on `||` delimiter using `String.Split()`
2. Creates separate `RegularExpression` objects for each segment
3. Limits to `MaxSeriesCount` (2) expressions

**Test Input Processing:**
```
Input:  "CPU=(?<cpu>\\d+)||MEM=(?<mem>\\d+)"
Output: [
    RegularExpression("CPU=(?<cpu>\\d+)"),
    RegularExpression("MEM=(?<mem>\\d+)")
]
```

### 3. TryGetMatchForRecord() - Multiple Expressions Path ✅
**Location:** `GraphViewModel.cs` lines 461-511

**Logic for expressions.Length > 1 (lines 488-507):**
```csharp
for (var index = 0; index < expressions.Length && index < MaxSeriesCount; index++)
{
    IDictionary<string, string> matches = expressions[index].GetKeyValuePairs(inputString);
    
    if (matches.Any())
    {
        var matchValue = matches.First().Value;
        if (index == 0)
            hasFirstValue = float.TryParse(matchValue, out value1);
        else
            hasSecondValue = float.TryParse(matchValue, out value2);
    }
}
```

**Processing for Test Data:**

**Record 1: "CPU=5"**
- index=0: expression[0].GetKeyValuePairs("CPU=5") → {["cpu", "5"]} → value1=5.0
- index=1: expression[1].GetKeyValuePairs("CPU=5") → {} (no match) → value2=NaN
- Returns: value1=5.0, value2=NaN ✅

**Record 2: "MEM=10"**
- index=0: expression[0].GetKeyValuePairs("MEM=10") → {} (no match) → value1=NaN  
- index=1: expression[1].GetKeyValuePairs("MEM=10") → {["mem", "10"]} → value2=10.0
- Returns: value1=NaN, value2=10.0 ✅

### 4. GetSeries() ✅
**Location:** `GraphViewModel.cs` lines 613-679

**Logic:**
```csharp
foreach (IRecord record in records)
{
    if (TryGetMatchForRecord(expressions, record.Content, out var value1, out var value2))
    {
        if (!float.IsNaN(value1))
            values1.Add(new DateTimePoint(record.CreatedAt, value1));
        if (!float.IsNaN(value2))
        {
            values2.Add(new DateTimePoint(record.CreatedAt, value2));
            hasSecondSeries = true;
        }
    }
}
```

**Processing Flow:**
1. Record 1: value1=5.0, value2=NaN → Add to values1, hasSecondSeries=false
2. Record 2: value1=NaN, value2=10.0 → Add to values2, hasSecondSeries=true

**Result:**
- values1: 1 point ✅
- values2: 1 point ✅  
- hasSecondSeries: true ✅
- Total series created: 2 ✅

### 5. GetSeriesNames() ✅
**Location:** `GraphViewModel.cs` lines 569-611, 513-567

**Logic:**
Extracts named groups from regex matches to use as series names.

**Processing:**
- Record 1 "CPU=5" matches expression[0] → seriesNames[0] = "cpu"
- Record 2 "MEM=10" matches expression[1] → seriesNames[1] = "mem"

**Result:** ["cpu", "mem"] ✅

## Simulation Test
Created standalone C# program that replicates the exact test scenario:

**Result:** ✓ **TEST WOULD PASS**

```
Testing regex: CPU=(?<cpu>\d+)||MEM=(?<mem>\d+)

Parsed into 2 expressions:
  Expression[0]: CPU=(?<cpu>\d+)
  Expression[1]: MEM=(?<mem>\d+)

Processing records:
  Record 1: 'CPU=5'
    Expression[0] matched: group 'cpu' = '5'
    Result: value1=5, value2=NaN

  Record 2: 'MEM=10'
    Expression[1] matched: group 'mem' = '10'
    Result: value1=NaN, value2=10

Final series:
  Series 1 has 1 points: [5]
  Series 2 has 1 points: [10]
  hasSecondSeries: True

Total series created: 2
✓ TEST WOULD PASS
```

## Conclusion

The implementation is **functionally correct**. All components work together properly to:
1. Split the regex expression on `||`
2. Match different records against different expressions
3. Extract named group values
4. Create two separate series with appropriate names and data

## Recommendations

1. ✅ **Run test on Windows CI** - Test requires WPF and can only run on Windows
2. ✅ **Verify no environment issues** - Check for any Windows-specific path or configuration problems
3. ⚠️ **Consider defensive enhancements** (optional):
   - Add null checks (already present)
   - Add logging for debugging (already present via Log.Default)
   - Add comments explaining the multi-expression logic

## Test History
- **Added:** January 24, 2026 (commit 4b7cb81)
- **Status:** Newly added test, not yet validated on CI
- **Related Issue:** #615 (Fix automated test failures)

## Analysis Date
January 25, 2026

## Analyst Notes
Analysis performed on Linux environment where WPF tests cannot run. Code review, logic tracing, and C# simulation all indicate correct implementation. Actual test run on Windows CI required to confirm.
