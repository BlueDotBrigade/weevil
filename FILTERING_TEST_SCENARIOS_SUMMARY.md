# Filtering Test Scenarios - Comprehensive Coverage

## Overview

This document summarizes the comprehensive Gherkin test scenarios added to ensure that filtering behaves as intended according to the requirements specified in issue #649.

## Test Coverage

### Total Test Scenarios: 26 Filtering Tests
- **Passing: 22 scenarios (85%)**
- **Failing: 4 scenarios (15%)** - All failures are due to a known bug from issue #649

## Intended Filtering Behavior

The filtering system should behave according to these rules:

### 1. Basic Filtering Rules

| Scenario | Expected Behavior |
|----------|-------------------|
| No INCLUDE, no EXCLUDE | All records visible |
| Only INCLUDE | All matching records are visible |
| Only EXCLUDE | Start with all records, then hide records that match EXCLUDE filter |
| INCLUDE & EXCLUDE | Find all records matching INCLUDE, then hide records matching EXCLUDE |

### 2. Configurable Override Rules

The following configuration values can override the EXCLUDE filter behavior:

- **"Show Pinned" enabled**: Pinned records are ALWAYS visible, even if they match EXCLUDE filter
- **"Show Bookmarks" enabled**: Bookmarked records are ALWAYS visible, even if they match EXCLUDE filter

**Important**: These overrides apply when filters exist. When NO filters exist and these options are enabled, ONLY pinned/bookmarked records should be visible.

## Test Scenarios by Category

### Category 1: Basic Filtering (8 scenarios - All Passing ✅)

1. ✅ **Logical OR operator** - Multiple expressions with OR
2. ✅ **Include filter displays specific keywords** 
3. ✅ **Exclude filter hides specified terms**
4. ✅ **Exclude overrides include** - When both filters applied
5. ✅ **Built-in filter alias #IPv6**
6. ✅ **No filters displays all records**
7. ✅ **Only include filter** - Shows matching records
8. ✅ **Only exclude filter** - Hides matching records from all

### Category 2: Show Pinned/Bookmarks with NO Filters (4 scenarios - All Failing ❌)

These scenarios test the specific bug from issue #649:

9. ❌ **Show Pinned ON, no filters** - Should show ONLY pinned records
   - Expected: 3 records | Actual: 387 records
   - Bug: Shows ALL records instead of only pinned ones

10. ❌ **Show Bookmarks ON, no filters** - Should show ONLY bookmarked records
    - Expected: 2 records | Actual: 387 records
    - Bug: Shows ALL records instead of only bookmarked ones

11. ❌ **Both options ON, no filters** - Should show pinned OR bookmarked records
    - Expected: 4 records | Actual: 387 records
    - Bug: Shows ALL records instead of only special ones

12. ❌ **Bookmarked with exclude filter** (existing test)
    - Expected: 3 records | Actual: 351 records
    - Bug: Exclude filter works, but bookmarked records not kept visible

### Category 3: Show Pinned/Bookmarks with INCLUDE Filters (4 scenarios - All Passing ✅)

13. ✅ **Show Pinned OFF, include filter** - Pinned records not shown unless they match
14. ✅ **Show Bookmarks OFF, include filter** - Bookmarked records not shown unless they match
15. ✅ **Show Pinned ON, include filter** - Pinned records always visible (existing test)
16. ✅ **Show Bookmarks ON, include filter** - Bookmarked records always visible (existing test)

### Category 4: Show Pinned/Bookmarks with EXCLUDE Filters (4 scenarios - All Passing ✅)

17. ✅ **Show Pinned OFF, exclude filter** - Pinned records ARE excluded if they match
18. ✅ **Show Bookmarks OFF, exclude filter** - Bookmarked records ARE excluded if they match
19. ✅ **Show Pinned ON, exclude filter** - Pinned records always visible even if excluded
20. ✅ **Show Bookmarks ON, exclude filter** - Bookmarked records always visible even if excluded

### Category 5: Combined INCLUDE & EXCLUDE Filters (4 scenarios - All Passing ✅)

21. ✅ **Include+Exclude, Show Pinned OFF** - Pinned records excluded if they match
22. ✅ **Include+Exclude, Show Bookmarks OFF** - Bookmarked records excluded if they match
23. ✅ **Include+Exclude, Show Pinned ON** - Pinned records always visible
24. ✅ **Include+Exclude, Show Bookmarks ON** - Bookmarked records always visible

## Known Issues

### Issue #649 Bug - Root Cause

The 4 failing tests expose a bug in `FilterStrategy.CanKeep()` method at lines 119-122:

```csharp
if (_inclusiveFilter.Count == 0 && _exclusiveFilter.Count == 0)
{
    canKeepRecord = true;  // ❌ BUG: Returns true for ALL records
}
```

**Problem:** When NO filters exist, the method immediately returns `true` without checking if "Show Pinned" or "Show Bookmarks" options are enabled. This causes ALL records to be shown instead of only pinned/bookmarked records.

**Required Fix:** Check special record status (pinned/bookmarked) BEFORE the "no filters" check:

```csharp
// Check special records first
if (_includePinned && record.Metadata.IsPinned) 
{
    return true;
}
if (_includeBookmarks && hasBookmark)
{
    return true;
}

// Then handle no filters case
if (_inclusiveFilter.Count == 0 && _exclusiveFilter.Count == 0)
{
    // Only show all records if special options are OFF
    return !(_includePinned || _includeBookmarks);
}
```

### Unit Tests vs Feature Tests

**Note:** The unit tests in `FilterStrategyTests.cs` pass because they directly test the `FilterStrategy` class with mocked data. However, the feature tests use the actual `Engine` which contains the buggy code. This discrepancy suggests the fix documented in `ISSUE_649_STEPS_TO_REPRODUCE.md` may not have been fully integrated into the main codebase.

## Test Data Reference

The test scenarios use `Droid.log` with the following characteristics:

- **Total records:** 387
- **Severity breakdown:**
  - Info: 37
  - Debug: 6
  - Trace: 338
- **Content categories:**
  - Diagnostics: 102
  - Directives: 7
- **Calculated values:**
  - Info excluding Directives: 30
  - Records excluding Trace: 49
  - Trace OR Info: 375
  - (Trace OR Info) excluding Diagnostics: 275

## Next Steps

1. **Fix the FilterStrategy bug** - Implement the required fix in `FilterStrategy.CanKeep()` method
2. **Verify all tests pass** - Run the feature tests to confirm all 26 scenarios pass
3. **Consider additional edge cases** - Such as:
   - Pinned AND bookmarked records (same record has both flags)
   - Empty filter strings vs null
   - Case sensitivity with special options
   - Multiple regions/insights with filters

## References

- **Issue:** BlueDotBrigade/weevil#649
- **Related Document:** `ISSUE_649_STEPS_TO_REPRODUCE.md`
- **Feature File:** `Tst/BlueDotBrigade.Weevil.Core-FeatureTests/Filter/Filtering.feature`
- **Step Definitions:** 
  - `FilteringSteps.cs`
  - `FilterOptionSteps.cs`
- **Implementation:** `Src/BlueDotBrigade.Weevil.Core/Filter/FilterStrategy.cs`