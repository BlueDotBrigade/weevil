# The Spelling Rule Ignores a Dictionary It Can't Recognize by Name

CA1704 ("Identifiers should be spelled correctly") comes from the Text.Analyzers package. It reads a custom dictionary only from an `AdditionalFiles` item whose file name matches `(?:dictionary|custom).*?\.(?:xml|dic)$`, ignoring case. Weevil's was named `DomainSpecificTerminology.xml`, so CA1704 never read it: `Ui` and `Tsv` were reported although the file listed them. Renaming it `DomainSpecificDictionary.xml` and listing it as `AdditionalFiles` in `Directory.Build.props` fixed that (#1271).

- Of the old Code Analysis dictionary format, it reads only the `Recognized` and `Unrecognized` words.
- The `CodeAnalysisDictionary` items left in the project files from FxCop did nothing: the .NET SDK ignores them.
- Only projects that reference Text.Analyzers run CA1704, which today is only `BlueDotBrigade.Weevil.Common`.

## A Word Just Added Can Still Be Reported

The compiler server that the .NET SDK and Visual Studio keep running can serve the dictionary as it was before an edit. While reviewing #1271, several builds in a row each reported a different word that the dictionary had just gained, until the server was stopped. After adding a word, run:

```bash
dotnet build-server shutdown
```

or restart Visual Studio. CI starts a new server for each run, so it isn't affected.
