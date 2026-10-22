# Virtual list proof of concept (#1014)

| | |
|---|---|
| Rows | 1,000,000 |
| Selection | WPF's own (`--wpf-selection`), as Weevil 2.12 |
| Computer | Intel(R) Core(TM) i9-9900K CPU @ 3.60GHz, 16 logical processors, 31.9 GB of RAM |
| Windows | Microsoft Windows 10.0.26300 |
| .NET | .NET 10.0.13 |
| Run | 2026-10-22 16:51 |

**4 of 9 steps with a target missed it.**

| Step | Time (ms) | Target | Met | GC pause (ms) | WPF asked for rows | IndexOf | Rows created | Managed heap (MB) | Result |
|---|---:|---|---|---:|---:|---:|---:|---:|---|
| Open: build the results and show the first rows | 348.9 |  |  |  | 0 | 0 | 0 | 22 | 1,000,000 rows |
| Drag the scroll bar to 50% | 56.5 | < 250 ms | yes | 0.0 | 452 | 0 | 30 | 20 | top row: line 550,000 |
| Drag the scroll bar to 100% | 30.9 | < 250 ms | yes | 0.0 | 436 | 0 | 29 | 21 | top row: line 1,099,969 |
| Drag the scroll bar to 0% | 40.1 | < 250 ms | yes | 0.0 | 434 | 0 | 0 | 23 | top row: line 1 |
| Ctrl+A (WPF's SelectAll) | 1,276.5 | < 100 ms; < 10 MB more | **no** | 334.2 | 1 | 0 | 999,970 | 328 | 1,000,000 selected; 311.1 MB more |
| Copy | 4.3 |  |  | 0.0 | 0 | 0 | 0 | 328 | Copy refused: 1,000,000 records selected, more than 100,000 |
| Select 1,000 rows again after a filter, one Add each (Weevil 2.12) | 230.9 | < 250 ms | yes | 5.8 | 501,500 | 0 | 0 | 520 | 1,000 selected |
| Select 2,000 rows again after a filter, one Add each (Weevil 2.12) | 640.3 | < 250 ms | **no** | 14.7 | 2,003,000 | 0 | 0 | 525 | 2,000 selected |
| Select 4,000 rows again after a filter, one Add each (Weevil 2.12) | 2,134.8 | < 250 ms | **no** | 58.7 | 8,006,000 | 0 | 0 | 578 | 4,000 selected |
| Select 8,000 rows again after a filter, one Add each (Weevil 2.12) | 7,965.2 | < 250 ms | **no** | 554.3 | 32,012,000 | 0 | 0 | 530 | 8,000 selected |
| Row view-models alive after a full garbage collection | 0.0 | < 1,000 | yes |  | 42,525,452 | 0 | 1,000,058 | 228 | 30 alive; 1,000,058 created during the run |

## Notes

- Selecting rows again stopped at 8,000: it took 8.0 s. Weevil 2.12 does this after every filter, for every selected row.
- WPF called GetEnumerator 2 time(s) and walked 1,000,000 rows with it, and called CopyTo 0 time(s), during the run. Walking the whole list would create every row.
- At the end: managed heap 228 MB, working set 794 MB. The filter results alone take 4 bytes per row: 4 MB with no filter.
- Shift+End and Ctrl+Shift+Plus (#806) are not run automatically with WPF's own selection: WPF reads the real state of the Shift and Ctrl keys. Try them by hand.
