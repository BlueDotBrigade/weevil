# Implementing an Internal Interface

- An `Interface` is essentially a contract that guarantees what the inheritor is agreeing to.
- IMHO the syntax for implementing an interface that is `internal` to a library is a little strange.  There are different options:
   1. `internal interface` + `internal class` + `public method`
      - [Why use a public method in an internal class?][InternalClassPublicMethod] by Eric Lippert
   2. Use extension method to hide internal interface on `public class`.
      - See: `IClonableInternallyExtensions`
      - Additional reading:
        - MSDN: [Explicit Interface Implementation][ExplicitInterfaceImplementation]
        - StackOverflow: [Why Explicit Implementation of a Interface can not be public?][InterfaceAccessibility]

[InternalClassPublicMethod]: https://stackoverflow.com/a/9302642/949681
[ExplicitInterfaceImplementation]: https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/interfaces/explicit-interface-implementation
[InterfaceAccessibility]: https://stackoverflow.com/a/1253277/949681
