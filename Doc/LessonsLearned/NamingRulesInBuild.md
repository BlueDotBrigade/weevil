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

The rule set turned off IDE0008 and IDE0009 the same way, so the projects that use it, the CLI, the GUI and most test projects, never reported them, even after #1270 and #1272 fixed them elsewhere. Making them errors in `.editorconfig` needed those rule set entries removed too.

Metalama is the exception. Its compiler bundles its own Roslyn, and fails with `LAMA0625` when the code style analyzers of the newest .NET SDK installed need a newer one. Without a `global.json`, the build uses the newest SDK on the machine: GitHub's Windows runners have SDK 11.0.100, whose analyzers need Roslyn 5.12, while `Metalama.Compiler` 2026.1.20 bundles 5.9. A machine with only SDK 10 builds fine, which is how #1270 first removed the exception and only CI caught it. So `Directory.Build.targets` turns `EnforceCodeStyleInBuild` off for every project Metalama compiles (`RoslynCompilerType` is `Metalama`): the GUI and its feature tests. Those projects check code style in the IDE only. With the rule set also turning IDE0008 off for them, the build never reported their 171 IDE0008 violations, so #1270 missed them at first. `dotnet format` finds and fixes a rule's violations there without Metalama; on Linux, set `EnableWindowsTargeting=true` in the environment first:

```bash
dotnet format style Src/BlueDotBrigade.Weevil.Gui/BlueDotBrigade.Weevil.Gui.csproj --diagnostics IDE0008 --severity info
```

Add `--verify-no-changes` to only report them.
