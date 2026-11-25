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

Metalama used to be the exception. An earlier version of its compiler could not load the .NET SDK's code style analyzers (`error LAMA0625`), so `Directory.Build.targets` turned `EnforceCodeStyleInBuild` off for the GUI and its feature tests, which then checked code style in the IDE only. With the rule set also turning IDE0008 off for them, the build never reported their 171 IDE0008 violations, so #1270 missed them. The Metalama compiler that Weevil restores from NuGet today (`Metalama.Compiler` 2026.1.20, through `Metalama.Patterns.Observability` 2026.1.30) runs the analyzers, in Debug and Release, so the exception was removed (#1270). That was checked with `dotnet build`; if LAMA0625 returns, for example after an SDK, Visual Studio or Metalama update, the compiler has lost the analyzers again. `dotnet format` fixes a rule's violations in a project; on Linux, set `EnableWindowsTargeting=true` in the environment first:

```bash
dotnet format style Src/BlueDotBrigade.Weevil.Gui/BlueDotBrigade.Weevil.Gui.csproj --diagnostics IDE0008 --severity info
```
