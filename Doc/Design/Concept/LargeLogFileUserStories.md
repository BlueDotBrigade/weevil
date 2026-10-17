# Large log files: user stories

Phases follow the [design concept](LargeLogFileStrategy.md). User stories for phases 1 and 2 are in GitHub; phase 3 is still a draft. The requirements below are the formal reference for each phase.

## Phase 1: faster parser

- [#975](https://github.com/BlueDotBrigade/weevil/issues/975): open large log files faster, with less memory. Done.
- [#976](https://github.com/BlueDotBrigade/weevil/issues/976): apply filters faster on large log files.

Requirements:

- When a log file is opened, Weevil shall produce the same records, line numbers and content as version 2.12.1.
- If a numeric field in a record header cannot be parsed, then Weevil shall treat the line as part of the previous record and continue loading.

## Phase 2: record index

- [#977](https://github.com/BlueDotBrigade/weevil/issues/977): opening a log from a section always starts at the right record.
- [#978](https://github.com/BlueDotBrigade/weevil/issues/978): keep a log file in much less memory, and reopen it without parsing it again.

Requirements:

- When a log file is opened, Weevil shall record the byte offset and line number of every record.
- When a sidecar is loaded, Weevil shall locate records, bookmarks, regions and sections by line number, and shall ignore stored byte offsets.
- When a sidecar is saved, Weevil shall write byte offsets taken from the record index.
- When a log file is opened from a start line, Weevil shall find the starting position from the record index.
- Weevil shall save the record index in the log file's folder, named after the log file with an `.idx` extension.
- When a log file is reopened and its saved index matches, Weevil shall reuse the saved index.
- When a copied index file matches its log file, Weevil shall reuse it, regardless of the file's path or timestamps.
- If the saved index does not match the log file, then Weevil shall rebuild the index.
- If the index file cannot be written, then Weevil shall continue without saving it.
- If the log file was opened from a compressed archive, then Weevil shall not save the index file.
- When a reopened log file has grown, Weevil shall index only the content after the start of the last indexed record.

## Phase 3: disk mode (draft)

- As an analyst, I want to choose whether a log file is kept in RAM or read from disk, so that I can open log files larger than my computer's memory.

Requirements:

- When the Open As dialog is shown, Weevil shall preselect RAM.
- While a log file is open in disk mode, Weevil shall read record content from the log file on demand.
- If RAM is selected and the file plus its index would exceed available memory, then Weevil shall warn the analyst and suggest disk mode.
- If disk mode is selected for a file on a network drive, then Weevil shall warn that filtering may be slow.
- While a log file is open in disk mode, Weevil shall filter a 20 GB log file in under 10 seconds on an NVMe drive.
