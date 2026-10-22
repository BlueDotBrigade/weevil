# Virtual list proof of concept (#1014)

| | |
|---|---|
| Rows | 1,000,000 |
| Selection | WPF's own (`--wpf-selection`), as Weevil 2.12 |
| Computer | Intel(R) Core(TM) i9-9900K CPU @ 3.60GHz, 16 logical processors, 31.9 GB of RAM |
| Windows | Microsoft Windows 10.0.26300 |
| .NET | .NET 10.0.13 |
| Run | 2026-10-22 16:32 |

**Unfinished:** the run is still going, or it stopped during the step after the last one below.

**1 of 4 steps with a target missed it.**

| Step | Time (ms) | Target | Met | WPF asked for rows | IndexOf | Rows created | Managed heap (MB) | Result |
|---|---:|---|---|---:|---:|---:|---:|---|
| Open: build the results and show the first rows | 346.1 |  |  | 0 | 0 | 0 | 22 | 1,000,000 rows |
| Drag the scroll bar to 50% | 52.9 | < 250 ms | yes | 452 | 0 | 30 | 20 | top row: line 550,000 |
| Drag the scroll bar to 100% | 30.6 | < 250 ms | yes | 436 | 0 | 29 | 21 | top row: line 1,099,969 |
| Drag the scroll bar to 0% | 41.2 | < 250 ms | yes | 434 | 0 | 0 | 23 | top row: line 1 |
| Ctrl+A (WPF's SelectAll) | 1,287.2 | < 100 ms; < 10 MB more | **no** | 1 | 0 | 999,970 | 328 | 1,000,000 selected; 311.1 MB more |

## Notes

