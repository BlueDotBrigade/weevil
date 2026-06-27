# Fix Summary for Issue #615 - DetectRepeatingRecords Test Failures

## Test Failure Analysis

### Failing Tests
Three related tests in `DetectRepeatingRecords.feature` were failing:
1. "Block found at start of results"
2. "Block found at end of results"
3. "Blocks found at start, middle and end of results"

All three tests expected flagged records but were getting 0 instead.

### Root Cause

The test step definition `WhenDetectingBothEdgesUsingTheRegularExpression` in `AnalysisSteps.cs` was mocking the wrong method.

**Before (Incorrect):**
```csharp
var parameterProvider = Substitute.For<IUserDialog>();
parameterProvider
    .ShowUserPrompt(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
    .Returns(regularExpression);
```

**Problem:** 
- The test mocked `ShowUserPrompt()`
- But `DetectRepeatingRecordsAnalyzer.Analyze()` actually calls `TryShowAnalysisDialog()`
- When `TryShowAnalysisDialog()` was called, it returned its default value (false), causing the analyzer to exit early with 0 results

### The Fix

Updated the mock to call the correct method:

**After (Correct):**
```csharp
var parameterProvider = Substitute.For<IUserDialog>();
parameterProvider
    .TryShowAnalysisDialog(Arg.Any<string>(), Arg.Any<string>(), out Arg.Any<string>())
    .Returns(x => { x[2] = regularExpression; return true; });
```

**Why this works:**
- `TryShowAnalysisDialog()` has an `out string` parameter (the third parameter, index 2)
- The mock sets this out parameter to the regex value: `x[2] = regularExpression`
- Returns `true` to indicate success
- The analyzer now receives the regex and proceeds with the analysis

### File Changed
- `Tst/BlueDotBrigade.Weevil.Core-FeatureTests/Analysis/AnalysisSteps.cs` (lines 48-60)

### Why Was This a Test Bug, Not a Library Bug?

1. The `DetectRepeatingRecordsAnalyzer` implementation is correct - it properly detects blocks of repeating records
2. The analyzer correctly calls `TryShowAnalysisDialog()` to get user input
3. The test was incorrectly mocking a different method (`ShowUserPrompt`)
4. Other tests in the codebase (e.g., `AnalysisShould.cs`) correctly mock `TryShowAnalysisDialog()`

### Expected Outcome

After this fix, the three failing DetectRepeatingRecords tests should pass:
- They will correctly provide the regex to the analyzer
- The analyzer will detect the blocks of repeating records
- The first and last record of each block will be flagged
- The test assertions will match the expected flagged record counts
