# About This Project

## Background

By design...
1. _Weevil_ can support vendor specific plugins that are used to load & analyze proprietary log files.
2. To promote loose coupling, _Weevil_ has no knowledge of it's plugins or their implementation details.
3. Plugins are developed completely independently of _Weevil_.
	- The reason: Plugins have vendor specific terminology and other proprietary information (e.g. credentials).

## Implementation Details

1. Plugins must **never** be committed to the _Weevil_ repository.
2. This project is used as a temporary cache to receive plugins as they are compiled.  The libraries (`*.dll`) are then consumed by the following projects:
	- `BlueDotBrigade.Weevil.Cli`
	- `BlueDotBrigade.Weevil.Gui`
	- `BlueDotBrigade.Weevil.Setup`