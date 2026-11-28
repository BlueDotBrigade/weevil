# Naming and Vocabulary

Two kinds of rule check Weevil's names. Each needs its own tool.

## Naming Style Rules

Naming style rules check the **shape** of a name: capitals, prefixes, suffixes, underscores. They don't know what the words mean.

- An interface starts with `I`: `IEngine`, `IRecord`.
- A type or member is in Pascal case: `RecordList`, `GetSelected`.

Roslyn enforces naming style. Weevil's rules are in [`.editorconfig`](../../.editorconfig), under "Naming rules", and the build reports them as IDE1006.

## Vocabulary Rules

Vocabulary rules check the **words** in a name: which ones the team prefers, and how they're written. They keep the code in the domain's language.

- Write `Tsv`, not `TSV`: `FileFormatType.Tsv`.
- Write `Ui`, not `UI`: `IRecord.WasGeneratedByUi`.
- Write `Metadata`, not `MetaData`: `MetadataType`.
- Use one word for a concept: Weevil says `Unselect`, so not `Deselect` as well.

FxCop enforced vocabulary through its custom dictionary. Roslyn doesn't: those rules weren't carried over. Two tools fill the gap.

## Text.Analyzers and BlueDotBrigade.Analyzers

| | Text.Analyzers | BlueDotBrigade.Analyzers |
|---|---|---|
| Rule | CA1704, "Identifiers should be spelled correctly" | Preferred and blocked terms |
| Reports | Any word it doesn't recognize | Only the terms you block |
| Its list | Words that are spelled correctly: [`DomainSpecificDictionary.xml`](../../DomainSpecificDictionary.xml) | Which word to use instead of another: `dsl.config.xml` |
| Catches | `comparision`, `Mesurement`, a name such as `Executor<I>` | `TSV`, `MetaData`, `Deselect` |
| Misses | Which spelling to prefer: once `Tsv` is listed, `TSV` passes too | A typo nobody blocked |
| In Weevil | Common only (#1271) | Every project (#1347) |

They aren't redundant. The first is a spell checker, the second a list of blocked terms, and each catches what the other misses.

### Configuring Text.Analyzers

- **In a project:** add or remove the `Text.Analyzers` package reference. Today only `BlueDotBrigade.Weevil.Common` has it, so only Common runs CA1704.
- **The dictionary:** `Directory.Build.props` gives `DomainSpecificDictionary.xml` to every project as an `AdditionalFiles` item. Text.Analyzers reads it only because its name contains "dictionary", and reads only its `Recognized` and `Unrecognized` words ([SpellingRuleIgnoresTheDictionary.md](../LessonsLearned/SpellingRuleIgnoresTheDictionary.md)).
- **On or off:** set the rule's severity in `.editorconfig`: `dotnet_diagnostic.CA1704.severity = error`, `warning` or `none`. Weevil sets `error`; the research projects keep `warning` in `Doc/Research/.editorconfig`.
- **One name only:** `[SuppressMessage("Naming", "CA1704:Identifiers should be spelled correctly", Justification = "…")]`. `IFile.Copy` keeps `destFileName`, the name `System.IO.File.Copy` uses.
- **After adding a word:** run `dotnet build-server shutdown`, or restart Visual Studio. The compiler server can keep reading the old file.

Add a word only when it is the clearest name: an established technical term, such as `Formatter` or `Func`, or a word of Weevil's own, such as `Checksums` or `Unselect`. Otherwise, rename.

The dictionary uses [Microsoft's format](https://learn.microsoft.com/previous-versions/visualstudio/visual-studio-2015/code-quality/how-to-customize-the-code-analysis-dictionary), from FxCop. [BlueDotBrigade/analyzers#15](https://github.com/BlueDotBrigade/analyzers/issues/15) describes each of its sections.

### Configuring BlueDotBrigade.Analyzers

- **In a project:** `Directory.Build.props` references the [`BlueDotBrigade.Analyzers`](https://www.nuget.org/packages/BlueDotBrigade.Analyzers) package, version 1.0.0, in every project, and gives each one [`dsl.config.xml`](../../dsl.config.xml) as an `AdditionalFiles` item. Version 1.0.0 doesn't add the file itself, so that item is needed, and a project's own `dsl.config.xml` or the `AnalyzerDslFileName` property has no effect (BlueDotBrigade/analyzers#13).
- **On or off:** set its rule's severity in `.editorconfig`: `dotnet_diagnostic.BDB001.severity = error`, `warning` or `none`. Weevil sets `error`; the research projects keep `warning` in `Doc/Research/.editorconfig`. Without the file, or with one it can't parse, it reports `BDB000` and checks nothing, so `Directory.Build.props` makes `BDB000` an error (`WarningsAsErrors`): a typo in `dsl.config.xml` fails the build. `.editorconfig` can't, because `BDB000` has no location in a file. Its README says `RC000`, which is out of date (BlueDotBrigade/analyzers#13).
- **One name only:** `#pragma warning disable BDB001`, or `[SuppressMessage]`, as for any Roslyn analyzer. Give the reason.

#### Adding a Term

1. Add a `term` to `dsl.config.xml`: the term to use, and each spelling or word to report instead. `case="sensitive"` reports only the spelling given; `insensitive` reports it in any case.

   ```xml
   <term prefer="Tsv" block="TSV" case="sensitive"/>
   <term prefer="Metadata" case="sensitive">
     <alias block="MetaData"/>
     <alias block="metaData"/>
   </term>
   ```

2. If CA1704 rejects the term, such as `Tsv`, also add it to `DomainSpecificDictionary.xml`. Version 1.0.0 can't read its terms from that file. A proof of concept that does is merged into the analyzer (BlueDotBrigade/analyzers#17), but no release includes it.
3. Build, rename what it reports, and run `dotnet build-server shutdown` if a change to the file seems ignored.

#### What It Misses

Version 1.0.0 has limits. Each link is to the analyzer's issue:

- **It matches text, not words** (BlueDotBrigade/analyzers#10): since `UI` is blocked, .NET's own `UInt32` would be reported in a name such as `ReadUInt32Value`. Before blocking a term, check that no other word in Weevil's names, or in .NET's, contains it.
- **It reports only the spelling given** (BlueDotBrigade/analyzers#11): `MetaData` doesn't report `metaData`, so list the camel-case form as an alias too.
- **It doesn't check every name:**
  - namespaces, type parameters, local functions and lambda parameters (BlueDotBrigade/analyzers#12, fixed in the analyzer's source, but no release includes it);
  - local variables other than plain declarations, such as `foreach`, `out`, pattern, `catch` and deconstructed variables;
  - the parameters of local functions and anonymous methods, tuple element names and labels;
  - generated code, such as the test names Reqnroll makes from a scenario's title. A title with a blocked term, such as "Filtering a TSV log", would fail a version that checks generated code.
- **An explicit interface implementation is checked by its full name,** such as `NsCSV.IThing.M`, so a blocked term in the interface's namespace or name is reported there. Only a suppression silences it.
