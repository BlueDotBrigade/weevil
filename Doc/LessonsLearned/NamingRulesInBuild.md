# Naming Rules Need Two Settings Before the Build Reports Them

An `.editorconfig` naming rule has its own severity:

```ini
dotnet_naming_rule.types_should_be_pascal_case.severity = warning
```

The IDE uses that severity, but `dotnet build` ignores it, even with `EnforceCodeStyleInBuild`. The build reports naming violations (IDE1006) only when the diagnostic also has a severity:

```ini
dotnet_diagnostic.IDE1006.severity = warning
```

A rule set can still hide them. `Weevil.ruleset`, used by the CLI, the GUI and the Core feature tests, set IDE1006 to `None`, which overrode `.editorconfig` for those projects.

Found in #983 by adding a method named `lowerCaseMethod` to Core and to the CLI and building with the .NET 10 SDK. Neither reported IDE1006 until the `dotnet_diagnostic` line was added; the CLI also needed the rule set entry removed.

Style options written as `option = value:warning` (for example `csharp_style_var_elsewhere = false:warning`) are reported by the build once `EnforceCodeStyleInBuild` is on.

Metalama is the exception. Its compiler cannot load the .NET SDK's code style analyzers (`error LAMA0625`), so `Directory.Build.targets` turns `EnforceCodeStyleInBuild` off for every project Metalama compiles (`RoslynCompilerType` is `Metalama`): the GUI and its feature tests. Those projects check code style in the IDE only.
