---
applyTo: "Src/BlueDotBrigade.Weevil.Gui/**"
---

# GUI Instructions

## Purpose

These instructions apply to the WPF desktop application (`BlueDotBrigade.Weevil.Gui`). The GUI must remain responsive and maintainable. Changes here affect the primary user-facing interface of the application.

## Architecture Pattern

- Follow the MVVM (Model-View-ViewModel) pattern throughout.
- ViewModels are internal classes annotated with `[Observable]` from `Metalama.Patterns.Observability` to automatically generate `INotifyPropertyChanged` implementations.
- Do not manually implement `INotifyPropertyChanged` in ViewModels; rely on the `[Observable]` attribute.
- XAML views bind to ViewModel properties. Do not place business logic in code-behind files.
- Apply MVVM best practices, SOLID principles, and Gang of Four patterns only when they meaningfully improve the code. Do not introduce these patterns if they make the source code unnecessarily complicated.

## UI Thread Safety

- Never block the UI thread. All long-running operations must execute on a background thread.
- Use `IUiDispatcher.Invoke()` to marshal results back to the UI thread after background processing.
- The `UiResponsivenessMonitor` detects UI thread delays; changes that cause delays will be caught in testing.
- Disable the responsiveness monitor only when a debugger is attached (existing pattern — do not change this logic).

## Inter-ViewModel Communication

- Use `IBulletinMediator` (implemented by `BulletinMediator`) for decoupled message passing between ViewModels.
- Subscribe to messages using `bulletinMediator.Subscribe<T>(recipient, callback)`.
- Publish messages using `bulletinMediator.Post<T>(bulletin)`.
- Do not create direct dependencies between ViewModels; route all cross-ViewModel communication through the mediator.

## Data Binding

- Maintain existing WPF data-binding patterns. Do not replace bindings with code-behind event handlers.
- Use value converters in the `Converters/` directory for type transformations in XAML.
- DataGrid columns for legend entries use 24px width with centered 12×12 `Rectangle` elements bound to `SeriesColor`.
- Graph series use the established Fluent Design color palette: Purple `#744DA9`, Red `#E74856`, Orange `#FF8C00`, Cyan `#0099BC`.

## Math Namespace Collision

- In `BlueDotBrigade.Weevil.Gui.Analysis`, use fully qualified `System.Math.*` calls (e.g., `System.Math.Min`, `System.Math.Round`) to avoid conflicts with any local `Math` namespace or type.

## Naming Conventions

- ViewModel classes are suffixed with `ViewModel` (e.g., `FilterViewModel`, `MainWindowViewModel`).
- XAML view files are suffixed with `View` or match their ViewModel name without the suffix (e.g., `StatusBarView.xaml`).
- Bulletin message classes are suffixed with `Bulletin` (e.g., `SourceFileOpenedBulletin`).
- Custom WPF controls live in the `Controls/` directory.
- Value converters live in the `Converters/` directory.

## Resource Management

- UI themes and style resources are defined in the `Themes/` directory and `Resources/` directory.
- Do not hardcode colors or font sizes in XAML; reference resource dictionary entries.
- Logging is configured via `nlog.config`; do not add additional logging configuration files.

## Feature Test Placement

- Add GUI feature tests only when the behavior cannot be verified through the core library's public API.
- GUI tests are appropriate for genuine end-user journeys, UI state transitions, and visual concerns.
- Do not duplicate core logic tests in the GUI test project.
