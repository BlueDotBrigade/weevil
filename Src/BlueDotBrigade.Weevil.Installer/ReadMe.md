# Weevil: Installer


## Creating MSI Using WiX

Current flow:

1. Build the installer project.
2. The installer stages a runtime layout under the project's intermediate output directory.
3. The staged layout is assembled from:
	- `BlueDotBrigade.Weevil.Gui`
	- `BlueDotBrigade.Weevil.Cli`
	- `BlueDotBrigade.Weevil.PowerShell`
	- PowerShell scripts
	- release documentation and licenses
	- plugins from `WEEVIL_PLUGINS_PATH`
4. Before WiX compiles, the installer checks `WEEVIL_PLUGINS_PATH` for an optional `TelemetryCredentials.wxs` file and copies it over the default installer credentials fragment when present.
5. WiX Heat harvests the staged `Bin`, `Doc`, and `Licenses` directories during the build.

This keeps Visual Studio debug outputs independent while generating installer payload authoring from the assembled runtime layout.

## How Heat Updates WiX Authoring

Heat scans the staged installer folders and generates temporary WiX source files in the installer project's `obj` directory. Those generated `.wxs` files contain the file-to-component mappings that WiX compiles into the MSI.

Sequence:

1. The installer project stages `Bin`, `Doc`, and `Licenses` into the intermediate layout.
2. Heat scans each staged directory.
3. Heat generates harvested `.wxs` files in `obj` for those directories.
4. WiX compiles the generated `.wxs` files together with the hand-authored installer files.

Current generated files:

- `_generated_weevil_bin_files_dir.wxs`
- `_generated_documentation_doc_files_dir.wxs`
- `_generated_documentation_license_files_dir.wxs`

## Upgrading to WiX Toolset 7

Short version:

1. Upgrade all WiX packages together to `7.0.0`
	- `WixToolset.Sdk`
	- `WixToolset.UI.wixext`
	- `WixToolset.Util.wixext`
	- `WixToolset.Netfx.wixext`
2. Remove `WixToolset.Heat`
3. Replace Heat-based harvesting with WiX 7 `Files`-based authoring
4. Build the MSI and fix any schema / MSBuild differences

What is different?

- `Heat` is deprecated in WiX 6 and removed in WiX 7
- WiX 7 expects newer file authoring patterns, especially `Files` instead of Heat-generated `.wxs`
- Some package / tooling behavior changes come with the major-version upgrade, so the installer project should be validated end-to-end

What is the benefit?

- No more `HEAT5149` warnings
- Simpler installer maintenance because generated Heat output is no longer needed
- Better long-term support because the installer follows the current WiX direction
- Easier future upgrades once the project is off deprecated tooling

Recommended migration for Weevil:

- Keep the current staged runtime layout
- Upgrade WiX packages to 7
- Replace Heat harvests for `Bin`, `Doc`, and `Licenses` with `Files` authoring
- Rebuild and test install / uninstall
