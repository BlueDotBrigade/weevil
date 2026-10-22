# Virtual list proof of concept (#1014)

| | |
|---|---|
| Rows | 100,000,000 |
| Selection | Ranges |
| Computer | Intel(R) Core(TM) i9-9900K CPU @ 3.60GHz, 16 logical processors, 31.9 GB of RAM |
| Windows | Microsoft Windows 10.0.26300 |
| .NET | .NET 10.0.13 |
| Run | 2026-10-22 16:50 |

**2 of 23 steps with a target missed it.**

| Step | Time (ms) | Target | Met | GC pause (ms) | WPF asked for rows | IndexOf | Rows created | Managed heap (MB) | Result |
|---|---:|---|---|---:|---:|---:|---:|---:|---|
| Open: build the results and show the first rows | 409.9 |  |  |  | 0 | 0 | 0 | 400 | 100,000,000 rows |
| Drag the scroll bar to 25% | 59.6 | < 250 ms | yes | 0.0 | 452 | 0 | 30 | 397 | top row: line 27,500,000 |
| Drag the scroll bar to 50% | 46.2 | < 250 ms | yes | 0.0 | 524 | 0 | 31 | 400 | top row: line 55,000,000 |
| Drag the scroll bar to 75% | 39.8 | < 250 ms | yes | 0.0 | 406 | 0 | 31 | 402 | top row: line 82,500,000 |
| Drag the scroll bar to 100% | 32.4 | < 250 ms | yes | 0.0 | 436 | 0 | 29 | 397 | top row: line 109,999,969 |
| Drag the scroll bar to 0% | 42.9 | < 250 ms | yes | 0.0 | 434 | 0 | 0 | 399 | top row: line 1 |
| Page Down, 100 times from the middle | 121.2 | < 250 ms (the longest) | yes | 145.4 | 41,542 | 100 | 2,671 | 405 | average 70.5 ms, longest 121.2 ms |
| Scroll a random row into view, 1,000 times | 102.6 | < 250 ms (the longest) | yes | 1,331.8 | 424,528 | 1,000 | 31,256 | 405 | average 47.5 ms, longest 102.6 ms |
| Click a row | 0.8 | < 100 ms | yes | 0.0 | 0 | 0 | 0 | 405 | 1 selected in 1 range(s) |
| Ctrl+A | 20.6 | < 100 ms; < 10 MB more | yes | 0.0 | 0 | 0 | 0 | 396 | 100,000,000 selected in 1 range(s); -1.4 MB more |
| Shift+End from row 10 | 80.5 | < 100 ms | yes | 0.0 | 557 | 1 | 29 | 400 | 99,999,990 selected in 1 range(s) |
| Ctrl+click 1,000 random rows | 33.9 | < 250 ms | yes | 0.0 | 0 | 0 | 0 | 402 | 1,001 selected in 1,001 range(s) |
| Filter: every 2nd record, all rows selected | 767.5 | keep the selection < 1,000 ms; UI pause < 250 ms | yes | 14.0 | 1,363 | 0 | 25 | 597 | 50,000,000 rows; filter 500 ms, keep the selection 2 ms; 50,000,000 selected in 1 range(s); longest UI pause 244 ms |
| Filter: every 10th record, every 2nd record selected | 1,003.8 | keep the selection < 1,000 ms; UI pause < 250 ms | yes | 22.5 | 1,363 | 0 | 25 | 638 | 10,000,000 rows; filter 503 ms, keep the selection 129 ms; 10,000,000 selected in 1 range(s); longest UI pause 239 ms |
| Filter: all records, every 10th record selected (the worst case) | 659.4 | keep the selection < 1,000 ms; UI pause < 250 ms | **no** | 34.0 | 1,783 | 0 | 33 | 523 | 100,000,000 rows; filter 58 ms, keep the selection 281 ms; 10,000,000 selected in 10,000,000 range(s); longest UI pause 257 ms; 473 MB more |
| Filter: warnings and above, one row selected | 1,194.7 | keep the selection < 1,000 ms; UI pause < 250 ms | yes | 21.0 | 343 | 0 | 14 | 543 | 4,100,081 rows; filter 993 ms, keep the selection 0 ms; 0 selected in 0 range(s); longest UI pause 162 ms |
| Filter: all records, one row selected | 355.0 | keep the selection < 1,000 ms; UI pause < 250 ms | **no** | 16.7 | 1,525 | 0 | 31 | 933 | 100,000,000 rows; filter 66 ms, keep the selection 0 ms; 1 selected in 1 range(s); longest UI pause 258 ms |
| IndexOf, 1,000,000 calls over 100,000,000 rows | 284.3 | < 1 µs per call | yes |  | 0 | 1,000,000 | 0 | 933 | 284 ns per call |
| #806: Ctrl+Shift+Plus three times, from line 5 | 17.0 | lines 10, 20, 10; one row selected and focused each time | yes |  | 0 | 0 | 0 | 936 | line 10, 1 selected; line 20, 1 selected; line 10, 1 selected |
| Ctrl+Plus from the pin on line 46 to a pin near the end of the log | 141.5 | < 250 ms | yes | 19.0 | 521 | 1 | 31 | 930 | line 109,998,901 |
| Type a comment, fast then slow | 4,142.5 | no typed text lost; a change from elsewhere is shown | yes |  | 0 | 0 | 0 | 933 | typed "Fast typing then slow", stored "Fast typing then slow", pinned True; change from elsewhere shown: True |
| Unpin All, 100,003 pins | 2.1 | < 250 ms | yes | 0.0 | 0 | 0 | 0 | 939 | 0 rows on screen still pinned |
| Copy 50,000 rows | 99.6 |  |  | 36.4 | 0 | 0 | 0 | 426 | Copied 50,000 records (6,084,294 characters) in 89 ms |
| Copy every row | 0.8 | refused above 100,000 | yes | 0.0 | 0 | 0 | 0 | 426 | Copy refused: 100,000,000 records selected, more than 100,000 |
| Row view-models alive after a full garbage collection | 0.0 | < 1,000 | yes |  | 478,587 | 1,001,103 | 35,332 | 396 | 31 alive; 35,332 created during the run |

## Notes

- WPF called GetEnumerator 1 time(s) and walked 0 rows with it, and called CopyTo 0 time(s), during the run. Walking the whole list would create every row.
- WPF selected a row on its own 0 time(s), and the range selection undid it.
- At the end: managed heap 396 MB, working set 1,035 MB. The filter results alone take 4 bytes per row: 381 MB with no filter.
