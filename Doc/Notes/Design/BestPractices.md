# Weevil: Framework Design

## Naming Convention

The following is a grammar-based breakdown of naming conventions in C# that follows .NET and Microsoft design guidelines:

| **Category**   | **Typical Grammar**                   | **Description**                                              | **.NET Examples**                                |
| -------------- | ------------------------------------- | ------------------------------------------------------------ | ------------------------------------------------ |
| **Methods**    | **Verbs or Verb Phrases**             | Represent actions or behaviors                               | `ToString()`, `Dispose()`, `GetEnumerator()`     |
| **Properties** | **Nouns or Adjectives**               | Represent state, data, or characteristics                    | `Length`, `IsEnabled`, `Name`                    |
| **Classes**    | **Nouns**                             | Represent entities, concepts, or data structures             | `Stream`, `HttpClient`, `FileInfo`               |
| **Interfaces** | **Adjectival Nouns (start with "I")** | Describe a capability or contract (behavioral or structural) | `IDisposable`, `IEnumerable`, `IServiceProvider` |

These conventions help make C# code intuitive and self-documenting.

## Appendices

## Appendix A: Additional Reading

- Microsoft [Framework Design Guidelines][DesignGuidelines]

[DesignGuidelines]: https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/