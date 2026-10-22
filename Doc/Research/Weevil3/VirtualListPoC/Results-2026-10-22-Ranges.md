# Virtual list proof of concept (#1014)

| | |
|---|---|
| Rows | 100,000,000 |
| Selection | Ranges |
| Computer | Intel(R) Core(TM) i9-9900K CPU @ 3.60GHz, 16 logical processors, 31.9 GB of RAM |
| Windows | Microsoft Windows 10.0.26300 |
| .NET | .NET 10.0.13 |
| Run | 2026-10-22 16:31 |

**3 of 23 steps with a target missed it.**

| Step | Time (ms) | Target | Met | WPF asked for rows | IndexOf | Rows created | Managed heap (MB) | Result |
|---|---:|---|---|---:|---:|---:|---:|---|
| Open: build the results and show the first rows | 413.2 |  |  | 0 | 0 | 0 | 400 | 100,000,000 rows |
| Drag the scroll bar to 25% | 64.9 | < 250 ms | yes | 452 | 0 | 30 | 397 | top row: line 27,500,000 |
| Drag the scroll bar to 50% | 45.6 | < 250 ms | yes | 524 | 0 | 31 | 400 | top row: line 55,000,000 |
| Drag the scroll bar to 75% | 38.3 | < 250 ms | yes | 406 | 0 | 31 | 402 | top row: line 82,500,000 |
| Drag the scroll bar to 100% | 36.0 | < 250 ms | yes | 436 | 0 | 29 | 397 | top row: line 109,999,969 |
| Drag the scroll bar to 0% | 48.2 | < 250 ms | yes | 434 | 0 | 0 | 399 | top row: line 1 |
| Page Down, 100 times from the middle | 134.2 | < 250 ms (the longest) | yes | 41,542 | 100 | 2,671 | 405 | average 66.5 ms, longest 134.2 ms |
| Scroll a random row into view, 1,000 times | 116.0 | < 250 ms (the longest) | yes | 426,372 | 1,000 | 31,269 | 407 | average 43.6 ms, longest 116.0 ms |
| Click a row | 1.6 | < 100 ms | yes | 0 | 0 | 0 | 407 | 1 selected in 1 range(s) |
| Ctrl+A | 21.3 | < 100 ms; < 10 MB more | yes | 0 | 0 | 0 | 395 | 100,000,000 selected in 1 range(s); -1.4 MB more |
| Shift+End from row 10 | 96.6 | < 100 ms | yes | 551 | 1 | 29 | 400 | 99,999,990 selected in 1 range(s) |
| Ctrl+click 1,000 random rows | 28.6 | < 250 ms | yes | 0 | 0 | 0 | 402 | 1,001 selected in 1,001 range(s) |
| Filter: every 2nd record, all rows selected | 778.4 | keep the selection < 1,000 ms; UI pause < 250 ms | yes | 1,363 | 0 | 25 | 597 | 50,000,000 rows; filter 490 ms, keep the selection 2 ms; 50,000,000 selected in 1 range(s); longest UI pause 224 ms |
| Filter: every 10th record, every 2nd record selected | 892.5 | keep the selection < 1,000 ms; UI pause < 250 ms | yes | 1,363 | 0 | 25 | 638 | 10,000,000 rows; filter 488 ms, keep the selection 139 ms; 10,000,000 selected in 1 range(s); longest UI pause 237 ms |
| Filter: all records, every 10th record selected (the worst case) | 694.3 | keep the selection < 1,000 ms; UI pause < 250 ms | **no** | 1,783 | 0 | 33 | 523 | 100,000,000 rows; filter 66 ms, keep the selection 274 ms; 10,000,000 selected in 10,000,000 range(s); longest UI pause 295 ms; 473 MB more |
| Filter: warnings and above, one row selected | 1,164.6 | keep the selection < 1,000 ms; UI pause < 250 ms | yes | 343 | 0 | 14 | 543 | 4,100,081 rows; filter 960 ms, keep the selection 0 ms; 0 selected in 0 range(s); longest UI pause 165 ms |
| Filter: all records, one row selected | 372.2 | keep the selection < 1,000 ms; UI pause < 250 ms | **no** | 1,525 | 0 | 31 | 933 | 100,000,000 rows; filter 80 ms, keep the selection 0 ms; 1 selected in 1 range(s); longest UI pause 266 ms |
| IndexOf, 1,000,000 calls over 100,000,000 rows | 287.2 | < 1 µs per call | yes | 0 | 1,000,000 | 0 | 933 | 287 ns per call |
| #806: Ctrl+Shift+Plus three times, from line 5 | 62.0 | lines 10, 20, 10; one row selected and focused each time | yes | 0 | 0 | 0 | 936 | line 10, 1 selected; line 20, 1 selected; line 10, 1 selected |
| Ctrl+Plus from the pin on line 46 to a pin near the end of the log | 254.5 | < 250 ms | **no** | 521 | 1 | 31 | 930 | line 109,998,901 |
| Type a comment, fast then slow | 4,229.7 | no typed text lost; a change from elsewhere is shown | yes | 0 | 0 | 0 | 934 | box "" after the reset; typed "Fast typing then slow", stored "Fast typing then slow", pinned False; change from elsewhere shown: True |
| Unpin All, 100,003 pins | 2.6 | < 250 ms | yes | 0 | 0 | 0 | 939 | 0 rows on screen still pinned |
| Copy 50,000 rows | 142.8 |  |  | 0 | 0 | 0 | 426 | Copied 50,000 records (6,084,294 characters) in 129 ms |
| Copy every row | 1.0 | refused above 100,000 | yes | 0 | 0 | 0 | 426 | Copy refused: 100,000,000 records selected, more than 100,000 |
| Row view-models alive after a full garbage collection | 0.0 | < 1,000 | yes | 480,425 | 1,001,103 | 35,352 | 396 | 31 alive; 35,352 created during the run |

## Notes

- WPF enumerated the whole list 1 time(s) and called CopyTo 0 time(s) during the run. Either would create every row.
- WPF selected a row on its own 0 time(s), and the range selection undid it.
- At the end: managed heap 396 MB, working set 1,036 MB. The filter results alone take 4 bytes per row: 381 MB with no filter.
