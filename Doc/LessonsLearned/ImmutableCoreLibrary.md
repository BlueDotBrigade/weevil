# An Immutable Core Library

The original intent was to create a thread safe class library by making `BlueDotBrigade.Weevil.Core` entirely immutable.  Unfortunately there have been a few unexpected side effects, which in retrospect, seem obvious now:
- new object tree for `Open`, `Clear` and `Refresh`
  - which means the events have to be hooked up again after the operation
    - **PROBLEM** : having to re-register events is not intuitive

**Resolved in Weevil 3.0 (#990):** records are immutable, and what changes lives in the log's session or the engine's view. Callers subscribe to `IEngine` once per open log (#1011). Clear keeps the engine (#1018), and Reload keeps the `IEngine` and its events. Only Open builds a new engine.

Relates to:
- [Issue76][]

[Issue76]: https://github.com/BlueDotBrigade/weevil/issues/76
