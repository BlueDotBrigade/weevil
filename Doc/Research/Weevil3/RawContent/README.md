# Content Held as Raw Bytes: Checks (#1133)

**Question:** when records hold the log's bytes instead of a string (#1133, step 1 of the [#988 plan](../../../Plans/988-IndexingService.md)), is every record still identical, and are filters and Find as fast?

**Status (2026-10-29):** every record was identical on 120 logs, and filters were as fast or faster. A Find through every record of a UTF-16 log was slower. The review of #1134 found defects that are not fixed yet: see [its review comment](https://github.com/BlueDotBrigade/weevil/pull/1134#issuecomment-6506647118). Run these checks again after fixing them.

The tools here are throwaway: none of them is part of `Weevil-v2.sln`.

## Equivalence: Every Record Identical

[Equivalence](Equivalence/) opens each log, from the top and from start lines 2, 7 and 100, and writes every record's line number, facts, `HasContent` and `Content`. Build it twice, against `main` and against the branch, then compare the two outputs.

```bash
python3 Equivalence/GenerateLogs.py logs/gen          # 69 logs: UTF-8/16/32, both byte orders, LF/CR/CRLF, invalid bytes, logs over 16 MB
# Copy the test logs too: every *.log, *.txt, *.tsv and *.csv under Tst/**/.Daten, into logs/daten/<n>/.

git worktree add ../wt-main origin/main
dotnet build Equivalence/Harness.csproj -c Debug -p:Platform=x64 -p:WeevilRoot=<path to ../wt-main> -o out-main
dotnet build Equivalence/Harness.csproj -c Debug -p:Platform=x64 -p:WeevilRoot=<path to this repository> "-p:DefineConstants=NEWBUILD" -o out-new
dotnet out-main/Harness.dll dump-main.txt logs
dotnet out-new/Harness.dll dump-new.txt logs
cmp dump-main.txt dump-new.txt
```

- Build each copy in a folder of its own, because the two builds reference different Core projects.
- `NEWBUILD` also checks that `ContentBuffer.Read` returns exactly `Content`.
- Logs larger than 5 MB are compared by a hash of all their records.

**Result:** the outputs were byte-identical for 51 test logs and 69 generated logs, 480 opens in all, and `ContentBuffer` matched `Content` for every record.

## Timing: Filters and Find

[Timing](Timing/) opens a log, then times nine filters and two Finds. Each figure is the median of three runs after a warm-up run. Build it against each Core as above, without `NEWBUILD`, in Release.

The logs were the 10-million-record log from `Tst/BlueDotBrigade.Weevil.Core-PerformanceTests/.Daten/.Global/10MillionRecords.zip`, and a UTF-16 copy made with `iconv -f UTF-8 -t UTF-16LE` behind a `FF FE` byte order mark. The raw figures are in [Results-2026-10-29.tsv](Timing/Results-2026-10-29.tsv): the first block is one run of each build on each log, and the rest are two more runs on the UTF-16 log in alternating order.

| | UTF-8 log | UTF-16 log (2 GB) |
|---|---|---|
| Open | 37.3 s → 19.6 s | about the same |
| Filters | 22–69% faster | within noise, mostly faster |
| Find through every record, finding nothing | about the same | about 0.2 s slower (+40%) |

The timings vary by up to ±30% between runs on the 4-vCPU container used: compare runs made one after another.

**A log with many multi-line records and non-ASCII text:** `big_mixed_utf8.log` from `GenerateLogs.py` has 600,000 records. About a third are multi-line, with mixed line breaks, and every record has non-ASCII text. Each build ran twice, with line breaks widened by vectorized searches (#1134 review); the raw figures are in [Results-2026-10-29-MultiLine.tsv](Timing/Results-2026-10-29-MultiLine.tsv). Every filter kept the same records. Regular expressions took 0.03–0.10 s instead of 0.03–0.04 s, case-insensitive plain text took 0.79 s instead of 0.55 s, and a Find took about 0.03 s longer. Decoding text that is not ASCII is slower than decoding ASCII, and records with many lines are rejoined with `\r\n` when read.

## Reviewer Checks

[ReviewerChecks](ReviewerChecks/) holds the throwaway checks the #1134 review used. Run them again after fixing its findings.

- **[Tests](ReviewerChecks/Tests/):** MSTest classes to copy into `Tst/BlueDotBrigade.Weevil.Core-UnitTests` of a worktree. Delete them afterwards.
  - `ZzFuzzReaderTests`: random bytes, including byte order marks and invalid sequences, read with `LogLineReader` and `StreamReader.ReadLine`, with chunk sizes 1–64 and a stream that returns 1–3 bytes per read. Every line must match.
  - `ZzFuzzRecordTests`: random logs through `MultilineRecordParser`.
  - `ZzRecordDumpTests` and `ZzBigDumpTests`: dump every record, for several parsers, encodings and start lines, to compare `main` and the branch with `cmp.py`.
  - `ZzContractTests`, `ZzNullTests` and `ZzLimitTests`: reproduce the review's findings. A parser that returns `true` with `Record.Dummy` or `null`, and `UsingLimit`.
- **[Benchmarks](ReviewerChecks/Benchmarks/):**
  - `micro` times decoding one record in each encoding: UTF-16 big-endian and UTF-32 were 10–50 times slower than UTF-8.
  - `src` measures opening, filtering and Clear. Build it with `-p:WeevilRoot=<repository or worktree>`, plus `-p:Branch=true` for a Core that holds content as bytes.

## Compiling the GUI on Linux

The GUI's build events run Windows commands. To compile the GUI and its tests on Linux, without running them, replace those events with [BuildGuiOnLinux.targets](BuildGuiOnLinux.targets):

```bash
dotnet build Tst/BlueDotBrigade.Weevil.Gui-FeatureTests/BlueDotBrigade.Weevil.Gui-FeatureTests.csproj -c Debug -p:Platform=x64 \
  -p:EnableWindowsTargeting=true -p:CustomAfterMicrosoftCommonTargets=<full path to BuildGuiOnLinux.targets>
```
