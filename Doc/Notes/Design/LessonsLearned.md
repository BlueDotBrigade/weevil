# Weevil Design : Lessons Learned

## Sofware Architecture

### API Design

Internal Contracts

- An `Interface` is essentially a contract that guarantees what the inheritor is agreeing to.
- IMHO the syntax for implementing an interface that is `internal` to a library is a little strange.  There are different options:
   1. `internal interface` + `internal class` + `public method`
      - [Why use a public method in an internal class?][InternalClassPublicMethod] by Eric Lippert
   2. Use extension method to hide internal interface on `public class`.
      - See: `IClonableInternallyExtensions`
      - Additional reading:
        - MSDN: [Explicit Interface Implementation][ExplicitInterfaceImplementation]
        - StackOverflow: [Why Explicit Implementation of a Interface can not be public?][InterfaceAccessibility]


### An Immutable Libary

The original intent was to create a thread safe class library by making `BlueDotBrigade.Weevil.Core` entirely immutable.  Unfortunately there have been a few unexpected side effects, which in retrospect, seem obvious now:
- new object tree for `Open`, `Clear` and `Refresh`
  - which means the events have to be hooked up again after the operation
    - **PROBLEM** : having to re-register events is not intuitive

Relates to:
- [Issue76][]

[Issue76]: https://github.com/BlueDotBrigade/weevil/issues/76
[InternalClassPublicMethod]: https://stackoverflow.com/a/9302642/949681
[ExplicitInterfaceImplementation]: https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/interfaces/explicit-interface-implementation
[InterfaceAccessibility]: https://stackoverflow.com/a/1253277/949681