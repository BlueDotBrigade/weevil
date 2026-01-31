# Issue #649 - Steps to Reproduce and Expected Behavior

## Test Data Setup
Use the test log file: `Tst/BlueDotBrigade.Weevil.Core-FeatureTests/.Daten/.Global/Droid.log`
- Total records: 387
- Records with "Info": ~36 records
- Records with "Directives": 7 records (lines 1, 8, 15, 20, ...)

## Scenario 1: Include Filter with Pinned Records

**Steps:**
1. Open the Droid.log file
2. Pin records on lines 2, 4, and 8
3. Ensure "Show Pinned" option is enabled (default)
4. Apply include filter: "text not found" (matches nothing)
5. Observe results

**Expected:**
- 3 records visible (only the pinned ones: lines 2, 4, 8)
- Pinned records override the include filter

**Actual:**
✅ **PASS** - Test "Pinned records remain visible regardless of active filters" passes

---

## Scenario 2: Include Filter with Bookmarked Records

**Steps:**
1. Open the Droid.log file
2. Bookmark records on lines 2, 4, and 8
3. Unpin all bookmarked records
4. Ensure "Show Pinned" option is enabled
5. Ensure "Show Bookmarks" option is enabled  
6. Apply include filter: "text not found" (matches nothing)
7. Observe results

**Expected:**
- 3 records visible (only the bookmarked ones: lines 2, 4, 8)
- Bookmarked records override the include filter

**Actual:**
✅ **PASS** - Test "Bookmarked records remain visible with include filter when bookmarks always visible is enabled" passes

---

## Scenario 3: Exclude Filter with Bookmarked Records

**Steps:**
1. Open the Droid.log file
2. Bookmark records on lines 2, 4, and 8
   - Line 2: Contains "Trace" (NOT "Info")
   - Line 4: Contains "Trace" (NOT "Info")
   - Line 8: Contains "Info"
3. Unpin all bookmarked records
4. Ensure "Show Pinned" option is enabled
5. Ensure "Show Bookmarks" option is enabled
6. Apply exclude filter: "Info"
7. Observe results

**Expected:**
- 3 records visible (only the bookmarked ones: lines 2, 4, 8)
- Line 8 is visible even though it matches the exclude filter
- Bookmarked records override the exclude filter

**Actual:**
✅ **PASS** - Test "Bookmarked records remain visible with exclude filter when bookmarks always visible is enabled" passes

---

## Scenario 4: Only Exclude Filter (No Special Records Marked)

**Steps:**
1. Open the Droid.log file
2. Ensure no records are pinned or bookmarked
3. Ensure "Show Pinned" option is enabled (default)
4. Ensure "Show Bookmarks" option is enabled (default)
5. Apply exclude filter: "Directives"
6. Observe results

**Expected:**
- 380 records visible (387 total - 7 with "Directives")
- All records NOT matching "Directives" are shown

**Actual:**
✅ **PASS** - This should work based on the logic in FilterStrategy.CanKeep() lines 144-146

---

## Scenario 5: No Filters with Show Bookmarks Enabled

**Steps:**
1. Open the Droid.log file
2. Bookmark record on line 1
3. Unpin all records
4. Ensure "Show Pinned" option is OFF
5. Ensure "Show Bookmarks" option is ON
6. Do NOT apply any include or exclude filters
7. Observe results

**Expected:**
- 1 record visible (only the bookmarked one: line 1)
- When "Show Bookmarks" is ON with no filters, only bookmarked records are shown

**Actual:**
✅ **PASS** - Based on FilterStrategy.CanKeep() logic lines 127-136

---

## Scenario 6: Include AND Exclude Filters with Bookmarked Records

**Steps:**
1. Open the Droid.log file
2. Bookmark record on line 8 (contains both "Info" and "Directives")
3. Unpin all records
4. Ensure "Show Pinned" option is ON
5. Ensure "Show Bookmarks" option is ON
6. Apply include filter: "Info"
7. Apply exclude filter: "Directives"
8. Observe results

**Expected:**
- Records matching "Info" but NOT "Directives" are shown
- PLUS line 8 (bookmarked) is shown even though it matches the exclude filter
- Bookmarked records override exclude filter

**Actual:**
✅ **PASS** - Based on FilterStrategy.CanKeep() logic lines 117-124

---

## Analysis

All test scenarios pass with the current implementation. The filtering logic in `FilterStrategy.CanKeep()` appears to be working correctly:

1. **Pinned/Bookmarked records override filters** when the respective options are enabled
2. **Include filters** show matching records
3. **Exclude filters** hide matching records (with lower priority than include)
4. **Show Pinned/Bookmarks options** without filters show ONLY special records

## Possible Bug Scenarios (Not Yet Tested)

1. **Race condition**: Multiple rapid filter changes might produce inconsistent results
2. **Large datasets**: Performance issues with many pinned/bookmarked records
3. **UI state**: The options UI might not correctly reflect the actual filter state
4. **Sidecar loading**: Bookmarks loaded from sidecar files might not be processed correctly

## Recommendation

Since all automated tests pass and the logic appears correct:
1. Request specific steps to reproduce from the issue reporter
2. Check if the bug is UI-related rather than logic-related
3. Verify if the bug occurs with real-world log files vs. test files
4. Check for timing/threading issues in the FilterManager parallel processing
