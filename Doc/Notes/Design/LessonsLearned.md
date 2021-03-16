# Weevil Design : Lessons Learned

## Sofware Architecture

### An Immutable Libary

The original intent was to create a thread safe class library by making `BlueDotBrigade.Weevil.Core` entirely immutable.  Unfortunately there have been a few unexpected side effects, which in retrospect, seem obvious now:
- new object tree for `Open`, `Clear` and `Refresh`
  - which means the events have to be hooked up again after the operation
    - **PROBLEM** : having to re-register events is not intuitive

Relates to:
- [Issue76][]


[Issue76]: https://github.com/BlueDotBrigade/weevil/issues/76