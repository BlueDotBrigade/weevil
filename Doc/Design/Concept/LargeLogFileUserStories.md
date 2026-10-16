# Large log files: draft user stories

Status: draft, 2026-10-16. Phases follow the [design concept](LargeLogFileStrategy.md).

## Phase 1: faster parser

- As an analyst, I want large log files to open faster, so that I can start investigating sooner.
- As an analyst, I want Weevil to use less memory while opening a file, so that I can open larger files.

Requirements:

- When a log file is opened, Weevil shall produce the same records, line numbers and content as version 2.12.1.
- If a numeric field in a record header cannot be parsed, then Weevil shall treat the line as part of the previous record and continue loading.

## Phase 2: record index

- As an analyst, I want comments, bookmarks and regions to stay attached to the right records, so that my annotations remain correct.
- As an analyst, I want a log file I have opened before to reopen quickly, so that I do not wait for it to be read again.

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

## Phase 3: disk mode

- As an analyst, I want to choose whether a log file is kept in RAM or read from disk, so that I can open log files larger than my computer's memory.

Requirements:

- When the Open As dialog is shown, Weevil shall preselect RAM.
- While a log file is open in disk mode, Weevil shall read record content from the log file on demand.
- If RAM is selected and the file plus its index would exceed available memory, then Weevil shall warn the analyst and suggest disk mode.
- If disk mode is selected for a file on a network drive, then Weevil shall warn that filtering may be slow.
- While a log file is open in disk mode, Weevil shall filter a 20 GB log file in under 10 seconds on an NVMe drive.
