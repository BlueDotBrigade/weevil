# A Moniker Ignores the Character After Its Keyword

> Notes from #1268, 2026-11-18.

## Symptom

`@Elapsed<5000`, `@Elapsed=5000` and `@Elapsed>5000` all keep the same records: those more than 5 seconds after the previous one. `@ContentLength>128` keeps records of 128 characters or more.

## Cause

A moniker's parameter is whatever follows its keyword and one more character, which is meant to be `=` (`Moniker.GetParameter`). The character is skipped, not read, so `<` and `>` mean nothing. Each moniker compares one way only: `@Elapsed` keeps records over the value, and `@ContentLength` records at least that long.

## Fix

Help writes `@Elapsed>5000` and `@ContentLength>128`, which read naturally and work. `@Elapsed<5000` does not mean "less than": there is no moniker for that.
