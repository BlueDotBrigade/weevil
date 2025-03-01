# Testing: Best Practices

## How to test each layer?

The majority of functional tests should target the `BlueDotBrigade.Weevil.Core.dll` assembly, as it contains the critical business logic driving the application. Testing at this layer ensures stability across all interfaces (e.g command-line & WPF applications) and are less fragile compared to UI-focused tests.

**UI layer** functional tests of `ViewModels` in the `BlueDotBrigade.Weevil.Gui.dll` assembly should focus on UI-specific logic, like validation or state changes, and critical end-to-end scenarios to validate integration with the core assembly. However, these should remain a smaller portion due to their higher maintenance cost and redundancy with core tests.

**Recommended Ratio**: `~70-80%` of tests for the core assembly, `~20-30%` for the UI layer, ensuring robust coverage while minimizing redundancy.
