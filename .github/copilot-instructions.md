# Weevil: GitHub Copilot Instructions

## Repository Identity

Weevil is a .NET 9 log analysis platform.

- Primary UI is a WPF desktop application (`BlueDotBrigade.Weevil.Gui`).
- The repository includes core libraries, a CLI tool, PowerShell modules, plugins, and a WiX installer.
- Log file operations are always non-destructive; the original file is never modified.
- Persisted state is stored in XML sidecar files.

## Directory Structure

```
Src/BlueDotBrigade.Weevil.Core        # Core log parsing, filtering, and analysis logic
Src/BlueDotBrigade.Weevil.Common      # Shared infrastructure used across projects
Src/BlueDotBrigade.Weevil.Gui         # WPF desktop application
Src/BlueDotBrigade.Weevil.Cli         # Command-line interface
Src/BlueDotBrigade.Weevil.PowerShell  # PowerShell tooling
Src/BlueDotBrigade.Weevil.Plugins     # Plugin infrastructure
Src/BlueDotBrigade.Weevil.Installer   # WiX installer
Src/BlueDotBrigade.Weevil.TestTools   # Shared test helpers and utilities
Src/BlueDotBrigade.Weevil.Windows     # Windows-specific platform abstractions
Tst/                                  # Unit tests, feature tests, and functional tests
Doc/                                  # Documentation, design notes, and UI style guides
```

## Architectural Rules

- Prefer small, targeted changes. Avoid broad refactors unless explicitly required.
- Do not introduce unnecessary abstractions or expand method semantics beyond what is needed.
- Preserve existing filter semantics; changes to filter logic require explicit approval.
- Maintain XML sidecar compatibility; do not change serialization formats without a migration plan.
- Avoid introducing tight coupling between the core library and plugins.
- Never modify the original log file; all operations must remain non-destructive.
- Preserve UI responsiveness; do not block the UI thread.
- Maintain existing WPF data-binding patterns.

## Build and Test Commands

CI builds and tests using .NET 9, Debug configuration, x64 platform on Windows.

```
dotnet restore Weevil-v2.sln

dotnet build Weevil-v2.sln --configuration Debug -p:Platform=x64 --no-restore

dotnet test Weevil-v2.sln --configuration Debug -p:Platform=x64 --no-build
```

> **Note:** The GUI project contains post-build scripts and must be built on Windows.
> All projects using `InternalsVisibleTo` restrict it to DEBUG builds, so CI always uses `--configuration Debug`.

## Testing Expectations

- Bug fixes must include regression tests.
- Prefer behavior-oriented tests over implementation-specific assertions.
- Scenario-based tests use the Reqnroll framework (BDD style).
- Unit tests use the MSTest framework.
- Place the bulk of feature tests in `Tst/BlueDotBrigade.Weevil.Core-FeatureTests`.
- Add GUI feature tests only for genuine end-user journeys or UI-specific behavior.
- Test method naming convention: `GivenCondition_WhenAction_ThenExpectedResult`.

## Global Coding Conventions

- Follow Microsoft .NET Framework Design Guidelines for naming.
- Methods use verb or verb-phrase names. Properties use noun or adjective names.
- Interfaces are prefixed with `I` and describe a capability or contract.
- Math calculators round computed values to 3 decimal places using `System.Math.Round(..., 3)`.
- In namespaces named `Math` (e.g., `BlueDotBrigade.Weevil.Math`), use fully qualified `System.Math.*` calls to avoid collision with the namespace name.

## Do Not Modify

- `.github/agents/` — contains agent-specific instructions; do not read or modify.
- Original log files opened by the application.
- Generated WiX installer artifacts.
