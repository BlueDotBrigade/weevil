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
| In Weevil | Common only (#1271) | Not yet (#1347) |

They aren't redundant. The first is a spell checker, the second a list of blocked terms, and each catches what the other misses.

### Configuring Text.Analyzers

- **In a project:** add or remove the `Text.Analyzers` package reference. Today only `BlueDotBrigade.Weevil.Common` has it, so only Common runs CA1704.
- **The dictionary:** `Directory.Build.props` gives `DomainSpecificDictionary.xml` to every project as an `AdditionalFiles` item. Text.Analyzers reads it only because its name contains "dictionary", and reads only its `Recognized` and `Unrecognized` words ([SpellingRuleIgnoresTheDictionary.md](../LessonsLearned/SpellingRuleIgnoresTheDictionary.md)).
- **On or off:** set the rule's severity in `.editorconfig`: `dotnet_diagnostic.CA1704.severity = error`, `warning` or `none`. Weevil sets `error`; the research projects keep `warning` in `Doc/Research/.editorconfig`.
- **One name only:** `[SuppressMessage("Naming", "CA1704:Identifiers should be spelled correctly", Justification = "…")]`. `IFile.Copy` keeps `destFileName`, the name `System.IO.File.Copy` uses.
- **After adding a word:** run `dotnet build-server shutdown`, or restart Visual Studio. The compiler server can keep reading the old file.

Add a word only when it is the clearest name: an established technical term, such as `Formatter` or `Func`, or a word of Weevil's own, such as `Checksums` or `Unselect`. Otherwise, rename.

### The Dictionary's Format

The format comes from FxCop. Every element, with names in Weevil's style:

```xml
<Dictionary>
  <Words>
    <Unrecognized>
      <Word>Cnt</Word>
    </Unrecognized>
    <Recognized>
      <Word>Tsv</Word>
      <Word>Unselect</Word>
    </Recognized>
    <Deprecated>
      <Term PreferredAlternate="Metadata">MetaData</Term>
      <Term PreferredAlternate="Unselect">Deselect</Term>
    </Deprecated>
    <Compound>
      <Term CompoundAlternate="FileName">filename</Term>
    </Compound>
    <DiscreteExceptions>
      <Term>timestamp</Term>
    </DiscreteExceptions>
  </Words>
  <Acronyms>
    <CasingExceptions>
      <Acronym>CJK</Acronym>
    </CasingExceptions>
  </Acronyms>
</Dictionary>
```

| Element | Means | FxCop rule | Text.Analyzers reads it |
|---|---|---|---|
| `Unrecognized` | Report this word as misspelled, even if it's a real word | CA1704 | Yes |
| `Recognized` | Accept this word as spelled correctly | CA1704 | Yes |
| `Deprecated` | Use the preferred word instead | CA1726 | No |
| `Compound` | Two words, so capitalize both: `FileName`, not `Filename` | CA1702 | No |
| `DiscreteExceptions` | One word, so don't split it: `Timestamp`, not `TimeStamp` | CA1702 | No |
| `CasingExceptions` | An acronym that keeps its own capitals | CA1709 | No |

Weevil's dictionary uses only `Recognized` today. Nothing enforces the last four sections: Roslyn didn't carry those rules over. `Deprecated` is the closest match to BlueDotBrigade.Analyzers' blocked terms, but it would list a preferred word such as `Unselect` twice.

### Configuring BlueDotBrigade.Analyzers

Weevil doesn't use it yet: #1347 tracks trying it, then adopting it. From its [README](https://github.com/BlueDotBrigade/analyzers):

- **In a project:** add the [`BlueDotBrigade.Analyzers`](https://www.nuget.org/packages/BlueDotBrigade.Analyzers) package reference.
- **The terms:** a `dsl.config.xml` beside the solution, or beside a project to override it for that project. The `AnalyzerDslFileName` property changes the file's name. Without the file, it reports `BDB000` and checks nothing. Its README says `RC000`, which is out of date (BlueDotBrigade/analyzers#13).

  ```xml
  <dsl>
    <term prefer="Tsv" block="TSV" case="sensitive"/>
    <term prefer="Metadata" block="MetaData" case="sensitive"/>
  </dsl>
  ```

- **On or off:** set its rule's severity in `.editorconfig`, or remove the package. `#pragma` and `[SuppressMessage]` work as for any Roslyn analyzer.
