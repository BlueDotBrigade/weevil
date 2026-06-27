Use the following context to help design a high-level implementation plan for AI interoperability support in the open-source .NET application **Weevil**.

# Project Context

Weevil is an open-source .NET log analysis application used by analysts and software developers to investigate complaints, isolate bugs, and extract useful information from log files.

Weevil already has its own clear purpose:
- opening and navigating large log files
- filtering records
- adding file-level remarks
- adding record-level comments
- flagging records through analysis tools
- highlighting/selecting records
- identifying patterns and trends
- showing dashboard-style insight about the log file

The goal is not to turn Weevil into an AI application.

The goal is to make Weevil’s existing investigative context available to external AI agents such as Claude, ChatGPT, Codex, or other MCP-compatible tools.

The intended user scenario is:

A developer or analyst has Weevil open while investigating a large log file. At the same time, they have an AI agent session open. The AI agent should be able to query Weevil for live runtime context, such as filtered records, flagged records, comments, selected records, file-level remarks, and dashboard insight.

This is different from simply building a command-line version of Weevil. A CLI could export data, but the more useful scenario is live interaction with the running Weevil process.

# Important Constraints

Weevil is open source and has limited development resources.

The implementation should follow an 80/20 approach:
- least implementation effort
- maximum practical benefit
- avoid over-engineering
- prefer a thin integration layer over a large AI subsystem
- keep Weevil focused on log analysis
- let external AI tools do the language reasoning

Log files may be very large:
- several gigabytes
- potentially 20 million records or more

Because of this, an AI agent should not be expected to read or scan the whole log file.

Instead, Weevil should expose structured, indexed, and navigable data so the AI agent can jump directly to relevant sections.

# Integration Options Discussed

Several possible integration approaches were discussed.

## Option 1: REST API

Expose a local HTTP API from the running Weevil process.

Example:
- Weevil runs a localhost-only API server.
- The AI agent queries endpoints such as `/records/flagged`, `/records/filter-results`, `/insights`, etc.
- Responses are JSON.

Advantages:
- simple to understand
- easy to test
- easy to call from many tools
- good 80/20 option

Disadvantages:
- AI agents may need custom configuration or helper tools to call it
- REST alone does not provide native agent/tool discovery

## Option 2: MCP Endpoint

Expose Weevil as a local MCP server.

The MCP server would provide tools/resources that an AI agent can discover and call.

Advantages:
- designed specifically for AI-agent interoperability
- supports tool discovery
- fits the Claude/Codex/agent use case well
- avoids inventing an AI-specific protocol from scratch

Disadvantages:
- may require learning MCP concepts
- may require more upfront design than a simple REST API
- .NET MCP support should be reviewed before implementation

## Option 3: gRPC Service

Expose strongly typed service methods from the running Weevil process.

Advantages:
- efficient
- strongly typed
- good for internal tooling

Disadvantages:
- less convenient for general AI agent integration
- likely more complex than needed for the first version

## Option 4: File-Based Export

Export Weevil context to JSON/XML files that an AI agent can read.

Advantages:
- simplest implementation
- no live server required
- works with existing sidecar/export concepts

Disadvantages:
- not truly live unless constantly refreshed
- less interactive
- AI agent may still need to locate and reload files
- weaker fit for the scenario where Weevil and the AI agent are running side-by-side

# Likely 80/20 Recommendation

Start with either:

1. a read-only local REST API, or
2. a small MCP server that internally calls the same read-only query services.

A practical approach may be:

- First design an internal Weevil query interface.
- Then expose that interface through one or more adapters:
  - REST adapter
  - MCP adapter
  - future CLI/export adapter

This keeps the core implementation clean and avoids coupling Weevil directly to one protocol.

# Core Design Principle

The AI integration should expose Weevil’s current investigative state.

It should not require the AI agent to understand Weevil internals.

It should provide:
- concise summaries
- indexes
- record metadata
- byte offsets
- raw text only when needed
- paged responses for large result sets

# Important Terminology

## Record

A record is a parsed log entry in Weevil.

When a record is returned to an AI agent, it should include:
- record ID or stable identifier
- original/raw text
- byte offset in the source log file
- line number
- timestamp/date-time if available
- whether the record is multi-line
- associated user comment, if any
- whether the record is flagged
- whether the record is pinned
- whether the record is highlighted/selected
- any other available metadata

The byte offset is especially important because it allows the AI agent or a future tool to jump directly to the source location without scanning gigabytes of log data.

## File-Level Remarks

Weevil supports high-level remarks about the log file.

These remarks should be exposed to the AI agent because they often capture the analyst’s current understanding of the investigation.

## Record Comments

Weevil supports comments on individual records.

These comments are important because they represent analyst-authored context. The AI agent should be able to query records with comments and include those comments in its reasoning.

## Flagged Records

A flagged record is a record marked by the most recently executed data analysis tool.

For example:
- Detect Falling Edges may flag records where a falling edge was detected.
- Detect Rising Edges may flag records where a rising edge was detected.
- Detect Data Transitions may flag records where a captured value changed.

Flagged records should be queryable by the AI agent.

## Highlighted / Selected Records

The user can select or highlight one or more records in the Weevil UI.

These records likely represent what the analyst is currently focused on, so the AI agent should be able to query them.

## Filter Results

Weevil supports inclusive/exclusive filters and other filtering capabilities.

The AI agent should be able to ask:

“Which records currently meet the active filter?”

The returned filter results should be paged and should include the original text, byte offset, and metadata for each record.

## Insights

In Weevil terminology, “insights” are not generic AI observations.

Insights are pre-canned application-level analysis results collected when a log file is opened or analyzed.

They are displayed to the user as a dashboard.

Each insight may include:
- metric name
- metric value
- plain-English description
- boolean value indicating whether the user’s attention is required

If user attention is required, it likely means a threshold was met or exceeded.

Example:
- memory exceeded a known threshold
- error count is unusually high
- timestamps appear out of order
- unusually large gaps exist between records

These dashboard insights should be exposed to the AI agent.

# Proposed Features

## 1. Expose a Table of Contents / Log Index

Weevil should expose a table of contents for the current log file.

This would act as an index that allows the AI agent to understand the structure of the log file without reading it entirely.

The table of contents should include meaningful sections or notable positions in the log file.

Each entry should include:
- label or description
- byte offset
- line number
- timestamp range if available
- record count if available
- category/type if available

The purpose is to let the AI agent jump to relevant regions of the file efficiently.

## 2. Expose Current Filter Results

The AI agent should be able to query the records that currently meet Weevil’s active filter.

This should support:
- pagination
- limits
- offsets/cursors
- inclusion of raw text
- inclusion of metadata
- byte offsets for each returned record

This lets the analyst ask the AI questions about the same subset of records they are viewing in Weevil.

## 3. Expose Flagged Records

The AI agent should be able to list records that were flagged by the most recently run data analysis tool.

Each flagged record should include:
- raw text
- byte offset
- line number
- timestamp
- user comment if available
- analysis-related metadata if available
- name of the analysis tool that flagged it, if available

## 4. Expose Highlighted / Selected Records

The AI agent should be able to query records currently selected or highlighted by the user.

This is important because selected records often indicate the analyst’s current focus.

Each highlighted record should include:
- raw text
- byte offset
- line number
- timestamp
- comments
- metadata

## 5. Expose Records With Comments

The AI agent should be able to query all records that have user comments.

This lets the AI agent summarize analyst findings and reason from human-authored notes.

## 6. Expose File-Level Remarks

The AI agent should be able to retrieve file-level remarks.

These remarks provide high-level investigation context.

## 7. Expose Dashboard Insights

The AI agent should be able to query Weevil’s insight/dashboard data.

Each insight should include:
- metric name
- metric value
- description
- attention-required boolean
- threshold information if available
- related record offsets if available

## 8. Expose Pinned Records

Pinned records were discussed earlier as another useful source of analyst intent.

Pinned records should likely be exposed as well, although they may be lower priority than:
- filter results
- flagged records
- highlighted records
- comments
- insights

## 9. Support Efficient Record Retrieval by Offset or ID

The AI agent should be able to ask for a specific record by:
- record ID
- line number
- byte offset

This allows the agent to retrieve surrounding context after seeing an index entry or a flagged record.

Useful query examples:
- get record by ID
- get record at byte offset
- get N records before and after this record
- get records in timestamp range
- get records near this offset

## 10. Support Context Windows Around Records

For large logs, the AI agent often needs nearby context.

Weevil should support queries such as:

“Return 10 records before and 10 records after this flagged record.”

The response should still include byte offsets and metadata.

# Example AI-Agent Questions To Support

The integration should eventually support questions like:

- “What records are currently visible in Weevil?”
- “Summarize the records that match the current filter.”
- “Show me the flagged records from the last analysis run.”
- “What records has the analyst commented on?”
- “What file-level remarks did the analyst add?”
- “What dashboard insights require attention?”
- “Show me the records currently selected by the user.”
- “What happened immediately before and after this flagged record?”
- “Summarize all records where a falling edge was detected.”
- “Are the selected records related to any dashboard insight?”
- “Do the commented records share a common timestamp range, thread ID, user ID, or error code?”

# Suggested API Shape

The exact protocol is undecided, but the conceptual API could include operations like:

- GetLogSummary
- GetTableOfContents
- GetCurrentFilter
- GetFilterResults
- GetFlaggedRecords
- GetHighlightedRecords
- GetPinnedRecords
- GetCommentedRecords
- GetFileRemarks
- GetInsights
- GetRecordById
- GetRecordByOffset
- GetRecordContext
- SearchRecords
- GetAnalysisSummary

# Example Record DTO

A returned record might look conceptually like this:

{
  "recordId": "123456",
  "lineNumber": 987654,
  "byteOffset": 123456789,
  "timestamp": "2026-05-28T14:32:10.123Z",
  "rawText": "Original log text here",
  "isMultiline": true,
  "comment": "Analyst note attached to this record",
  "isFlagged": true,
  "isPinned": false,
  "isHighlighted": true,
  "metadata": {
    "threadId": "42",
    "level": "ERROR",
    "source": "Example.Component"
  }
}

# Example Insight DTO

A dashboard insight might look conceptually like this:

{
  "name": "Memory Usage",
  "value": "92%",
  "description": "Memory usage exceeded the configured threshold.",
  "attentionRequired": true,
  "threshold": "90%",
  "relatedRecordIds": ["123456", "123457"],
  "relatedByteOffsets": [123456789, 123457111]
}

# Security Considerations

Because log files may contain sensitive information, the first version should probably be local-only.

Recommended first-version constraints:
- bind only to localhost
- read-only API
- no remote access
- no mutation operations
- no ability for AI agent to change comments, filters, or records
- clear user opt-in to enable AI integration
- visible indicator when AI access is enabled

Future versions could consider:
- API keys
- named-pipe transport
- permission scopes
- redaction/anonymization
- audit log of AI-agent queries

# Performance Considerations

The integration must be designed for very large files.

Important practices:
- never return unbounded result sets
- require pagination
- include byte offsets
- include cursors where appropriate
- avoid copying entire logs into memory
- expose summaries and indexes first
- allow the AI agent to request raw text only for targeted records
- support context windows around relevant records

# Suggested Phased Plan

## Phase 1: Internal Query Abstraction

Create an internal service/interface that exposes Weevil’s investigation state without committing to REST, MCP, gRPC, or files.

Example responsibilities:
- get current file summary
- get table of contents/index
- get current filter results
- get flagged records
- get highlighted records
- get records with comments
- get file-level remarks
- get insights
- get record context

This is the foundation.

## Phase 2: Read-Only Local API or MCP Proof of Concept

Implement the simplest useful external access layer.

Option A:
- local REST API returning JSON

Option B:
- local MCP server exposing Weevil tools/resources

The implementation should be read-only.

The first proof of concept should support:
- get log summary
- get dashboard insights
- get file remarks
- get highlighted records
- get flagged records
- get commented records
- get current filter results with pagination
- get record context around a record

## Phase 3: Improve Indexing / Table of Contents

Add or expose a table-of-contents/index feature so the AI agent can understand large log structure.

This should include byte offsets and timestamp ranges.

## Phase 4: MCP-Specific Polish

If MCP is selected or added after REST:
- expose tools with clear names and descriptions
- expose resources for summaries and indexes
- document setup instructions for Claude/Codex-compatible clients
- provide sample prompts
- provide a small demo scenario

## Phase 5: Optional Enhancements

Possible future additions:
- redaction support
- query audit log
- API authentication
- support for AI-suggested filters
- support for AI-generated summaries saved back into Weevil
- mutation endpoints for comments/remarks, only after the read-only model is proven safe
- richer analysis summaries
- correlation between insights and records

# Key Open Questions

- Should the first implementation be REST, MCP, or both?
- Does .NET 8/9 currently have a mature enough MCP server library for this use case?
- Should Weevil host the integration endpoint inside the desktop process, or should a companion process connect to Weevil?
- What is the safest default transport: localhost HTTP, named pipes, or MCP stdio?
- What fields are already available in Weevil’s record model?
- Does Weevil currently store byte offsets for each record?
- If byte offsets are not currently stored, where should they be captured?
- How should multi-line records be represented?
- How should result pagination/cursors be designed?
- Should AI integration be enabled per session, per file, or globally?

# Desired Outcome

Produce a high-level design document and implementation plan for adding AI-agent interoperability to Weevil.

The design should prioritize:
- minimal development effort
- practical usefulness
- live runtime access to Weevil’s investigative state
- support for very large log files
- read-only safety
- protocol flexibility
- eventual MCP compatibility

The most important idea is:

Weevil should remain the expert log analysis tool, while external AI agents should be able to query Weevil’s structured investigation context in order to help the analyst reason about the log file.