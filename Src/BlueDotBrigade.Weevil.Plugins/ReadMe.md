# Read Me

This project is used as a staging area for Weevil plugins.  With this in mind, it is important that that you...

1. Do **not** commit plugin assemblies in this directory to the Git repository.
    - Remember: plugins often contain proprietary data (e.g. vendor specific terminology, passwords, etc.)
2. Do **not** reference the plugin assemblies from Weevil projects.
    - The reason: to promote loose coupling between Weevil and it's plugins. 

## How It Works

Environment setup:

1. Download the Weevil source code from GitHub.
2. Create a Windows `WEEVIL_PLUGINS_PATH` environment variable that points to the`BlueDotBrigade.Weevil.Plugins` directory.

Modifying third-party plugin:

1. In a separate solution, create a new C# project for a vendor specific Weevil plugin.
2. Modify the post build step to copy the plugin's assemblies to `WEEVIL_PLUGINS_PATH`.
3. Add a new feature to the plugin.
4. Compile all third-party plugins so that the appropriate output is copied to `WEEVIL_PLUGINS_PATH`. 

Compiling Weevil:

1. A new feature is added to Weevil's core engine.
2. Software developer starts compiling Weevil's GUI project.
3. The build will result in the `BlueDotBrigade.Weevil.Plugins`  project compiling as well.
    - In reality, there is nothing to compile.
    - Because this is a `Microsoft.NET.Sdk` project, the compiler will automatically copy all *.dll files to the `Bin` directory.  There is no need for a post-build step.
    - This is needed so that the plugin assemblies (`*.dll`) can be consumed as "project output" when the Visual Studio Installer project is compiled.
4. After compiling the Weevil GUI the "post-build" step will copy the plugins from `WEEVIL_PLUGINS_PATH` to the appropriate directory.
    - This is done because during development, the GUI project needs it's own copy of the plugin assemblies.
5. When Weevil starts, the application will search the `Plugin` directory.

Creating an installation package:

1. Software developer starts compiling the Visual Studio Installer project.
1. The installer project will then copy the assemblies from the `Bin` directory to the installation package.

## Plugins and WPF

Weevil's core engine, and so the CLI and PowerShell, load the plugin assemblies directly in the plugins directory. Keep those free of WPF, so that every feature works headless, and in environments that can't run WPF.

A plugin uses Weevil's options in the Open As dialog unless it supplies its own (#1216). To supply them, put a separate assembly in a `Wpf` subdirectory (`Plugins\Wpf`) that exports `IOpenAsOptionsProvider` from `BlueDotBrigade.Weevil.Windows`, naming the plugin it is for. Only Weevil's window loads that subdirectory.

A provider is created off the UI thread, before the plugins in `Plugins` are loaded, so its constructor creates no WPF object and uses nothing from the plugin's own assembly; it does that work in `CreateOpenAsOptions` and `CreateOpenAsView`. Put any other assembly the provider needs beside it in `Plugins\Wpf`.

`IPlugin.CheckCompatibility` may be called on several threads at once, as the analyst selects logs, so a plugin keeps no state between calls.

A plugin assembly that fails to load, or a plugin whose constructor throws, is skipped, and Weevil's log says so: the other plugins still load.

## Additional Notes

- Because the `BlueDotBrigade.Weevil.Plugins` project is of type `Microsoft.NET.Sdk`, the plugin assemblies are automatically visible in the Visual Studio's Solution Explorer.