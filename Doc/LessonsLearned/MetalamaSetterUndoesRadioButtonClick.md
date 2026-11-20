# Metalama Can Undo a Radio Button Click

> Notes from #1215, 2026-11-17. Found in review; the WPF part still needs to be confirmed on Windows.

## Symptom

The Open As dialog had a RAM and a Disk radio button, each bound two-way to a `bool` on an `[Observable]` view model. Clicking Disk is likely to put RAM straight back, so the click appears to do nothing.

## Cause

Metalama wraps a hand-written setter. The woven setter compares the value written with what the getter returns, and raises `PropertyChanged` when they differ, whatever the setter's own code does:

```csharp
if (IsRamChosen_Source != value) { IsRamChosen_Source = value; OnPropertyChanged("IsRamChosen"); }
```

A setter that ignores `false`, so that only the button checked counts as a choice, still raises `PropertyChanged` when `false` is written to a property that reads `true`.

WPF then does the rest when Disk is clicked:

1. Checking Disk unchecks RAM in the same group, before Disk's own binding writes to the view model.
2. RAM's binding writes `IsRamChosen = false`. The mode is still RAM, so the getter still returns `true`, and Metalama raises `PropertyChanged`.
3. A binding whose source raises `PropertyChanged` while the binding is writing to it reads the source again. It reads `true` and checks RAM, which unchecks Disk.

## Fix

Bind both radio buttons to one enum property through `EnumToBooleanConverter`, as `FilterOptionsView` does. Its `ConvertBack` returns `Binding.DoNothing` for `false`, so the button being unchecked writes nothing. See `OpenAsOptionsViewModel.ChosenMode`.

`[NotObservable]` on the `bool` properties is no fix: they then stop being raised when the mode changes for another reason, such as Weevil's suggestion.

## In General

Under `[Observable]`, a setter that ignores some values, or stores something other than the value it was given, still raises `PropertyChanged` whenever its getter disagrees with the value written. Prefer a property whose getter returns what its setter was given.
