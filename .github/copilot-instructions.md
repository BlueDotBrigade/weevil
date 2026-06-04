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
- GUI ViewModels (`BlueDotBrigade.Weevil.Gui`) use the **Metalama.Patterns.Observability** aspect-oriented framework to implement `INotifyPropertyChanged`. Classes decorated with `[Observable]` (e.g. `FilterViewModel`, `MainWindowViewModel`, `StatusBarViewModel`) get `PropertyChanged` woven in at build time for plain auto-properties — do **not** add manual `OnPropertyChanged` calls or backing fields. Use `[NotObservable]` to opt out a specific member. **Caveat:** Metalama suppresses `PropertyChanged` when the new value equals the current; if a binding must refresh on every assignment (even to the same value), the auto-property approach is insufficient and a manual property is required.
- The majority of business logic must reside in `Core` so the application can run headless. Weevil's UI layer is interchangeable — it could be a WPF application, CLI, PowerShell module, or a test framework like Reqnroll.
- Apply the DRY (Don't Repeat Yourself) principle to code, configuration, and documentation. Avoid duplicating logic, data, or rules across multiple locations.
- Prefer simplifying repository configuration so all .csproj files are configured consistently when feasible (avoid special-case project settings unless required).
- When reorganizing Weevil build outputs, preserve a debugging workflow where both the GUI and CLI run against the latest built assemblies and include plugins from `WEEVIL_PLUGINS_PATH` in Visual Studio 2026, whether or not the debugger is attached.

## Design Principles

Favor simple, clear designs. Apply established principles and patterns only when they meaningfully improve the structure of the code:

- Low coupling / high cohesion
- SOLID design principles
- .NET flavour of the Gang of Four design patterns
- MVVM best practices (for GUI code)

These concepts should **not** be applied if they will make the source code unnecessarily complicated.

## Build and Test Commands

CI builds and tests using .NET 9, Debug configuration, x64 platform on Windows.

```
dotnet restore Weevil-v2.sln

dotnet build Weevil-v2.sln --configuration Debug -p:Platform=x64 --no-restore

dotnet test Weevil-v2.sln --configuration Debug -p:Platform=x64 --no-build
```

> **Note:** The GUI project contains post-build scripts and must be built on Windows.
> All projects using `InternalsVisibleTo` restrict it to DEBUG builds, so CI always uses `--configuration Debug`. If the build fails in this workspace, likely cause is a running Weevil instance; close any running Weevil instances before attempting a build.

## Testing Expectations

- Bug fixes must include regression tests.
- Prefer behavior-oriented tests over implementation-specific assertions.
- Scenario-based tests use the Reqnroll framework (BDD style).
- Unit tests use the MSTest framework.
- Place the bulk of feature tests in `Tst/BlueDotBrigade.Weevil.Core-FeatureTests`.
- Add GUI feature tests only for genuine end-user journeys or UI-specific behavior.
- Test method naming convention: `GivenCondition_WhenAction_ThenExpectedResult`.
- For test code in Tst projects, use `BlueDotBrigade.Weevil.TestTools.Data.R` to create fake records to keep tests readable, and prefer `FluentAssertions` over `MSTest Assert` where equivalent assertions exist.
- In Tst projects, unit test class names must use the `{ClassUnderTest}Tests` convention, and the .cs file name must match the test class name exactly.

## Global Coding Conventions

- Follow Microsoft .NET Framework Design Guidelines for naming.
- Methods use verb or verb-phrase names. Properties use noun or adjective names.
- Interfaces are prefixed with `I` and describe a capability or contract.
- Math calculators round computed values to 3 decimal places using `System.Math.Round(..., 3)`.
- In namespaces named `Math` (e.g., `BlueDotBrigade.Weevil.Math`), use fully qualified `System.Math.*` calls to avoid collision with the namespace name.
- Documentation and code comments must be clear and concise. Avoid visual noise that obscures intent.
- **Namespace alignment**: Place new classes in the same namespace you would expect to find them in the .NET Core library. For example: diagnostic and telemetry types belong in `BlueDotBrigade.Weevil.Diagnostics` (mirrors `System.Diagnostics`); SQL client implementation types belong in `BlueDotBrigade.Weevil.Data.SqlClient` (mirrors `Microsoft.Data.SqlClient`). This convention makes the codebase easier to navigate for developers already familiar with .NET Core.

## Documentation Conventions

- Use **user stories** to capture feature requests.
- Use **EARS syntax** to capture formalized requirements.
- Use **Gherkin scenarios** to capture test specifications.
- For documentation aimed at Weevil users (e.g. Help), prefer clear and concise explanations in plain English for engineers who may be new to the product.

## Do Not Modify

- `.github/agents/` — contains agent-specific instructions; do not read or modify.
- Original log files opened by the application.
- Generated WiX installer artifacts.
