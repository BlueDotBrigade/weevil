namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel.Composition.Hosting;
	using System.ComponentModel.Composition.Primitives;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using BlueDotBrigade.Weevil.Diagnostics;

	public class PluginFactory
	{
		private const string PluginDirectory = @"Plugins";
		private static readonly string _pluginDirectoryPath;

		/// <summary>
		/// The plugins, loaded the first time this factory is asked for one.
		/// </summary>
		private IReadOnlyList<IPlugin> _plugins;

		private object _pluginsLock;

		static PluginFactory()
		{
			var executingAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
			var executingAssemblyDirectory = Path.GetDirectoryName(executingAssemblyPath);
			_pluginDirectoryPath = Path.Combine(executingAssemblyDirectory, PluginDirectory);
		}

		/// <summary>
		/// The folder plugins are loaded from: only the assemblies directly in it, never those in its subfolders.
		/// </summary>
		/// <remarks>
		/// Weevil's window loads WPF assemblies, such as a plugin's Open As options, from a subfolder of its own (#1216), so Core, the CLI and PowerShell never load WPF.
		/// </remarks>
		public static string DirectoryPath => _pluginDirectoryPath;

		/// <summary>
		/// Returns the plugin that reads the log: the first whose <see cref="IPlugin.CheckCompatibility"/> accepts it, or Weevil's default plugin.
		/// </summary>
		/// <remarks>
		/// The plugins are loaded once for each factory. A plugin may read the log to tell whether it is one of its logs.
		/// </remarks>
		public IPlugin Create(string sourceFilePath)
		{
			return Create(sourceFilePath, LogSeverityType.Information, LogSeverityType.Warning);
		}

		/// <summary>
		/// Returns the plugin that reads the log, as <see cref="Create(string)"/> does, but says which it found only at Debug,
		/// for a caller that asks often, such as the Open As dialog each time the analyst selects a log (#1216).
		/// </summary>
		/// <remarks>
		/// Loading the plugins, the first time, still warns when the plugin folder is missing or empty, or a plugin can't be loaded.
		/// A plugin may read the log to tell whether it is one of its logs, so call it off the UI thread.
		/// </remarks>
		public IPlugin Find(string sourceFilePath)
		{
			return Create(sourceFilePath, LogSeverityType.Debug, LogSeverityType.Debug);
		}

		/// <summary>
		/// Creates the parts that the assemblies directly in <paramref name="folder"/> export as <typeparamref name="T"/>.
		/// </summary>
		/// <remarks>
		/// Each assembly is loaded on its own, so one that fails to load, for example one built against another version of Weevil or one that needs WPF in the CLI,
		/// is skipped with a warning that names it, rather than stopping every plugin and so every open (#1216). Each part is created on its own too, so one whose
		/// constructor throws is skipped with a warning. The assemblies loaded are composed together, so a part may import another assembly's.
		/// When they can't be composed, the warning says why, and none of their parts is returned.
		///
		/// The parts are created on the calling thread, and the container that created them is disposed before they are returned, which disposes a part that is <see cref="IDisposable"/>.
		/// </remarks>
		/// <param name="folder">The folder whose assemblies are loaded. A missing folder has none.</param>
		public static IReadOnlyList<T> LoadExports<T>(string folder)
		{
			if (!Directory.Exists(folder))
			{
				return Array.Empty<T>();
			}

			var catalogs = new List<ComposablePartCatalog>();

			foreach (var file in Directory.EnumerateFiles(folder, "*.dll").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
			{
				AssemblyCatalog catalog = null;

				try
				{
					catalog = new AssemblyCatalog(file);

					// Reads the assembly's types, which is where a type that can't be loaded fails.
					_ = catalog.Parts.ToList();
					catalogs.Add(catalog);
				}
				catch (BadImageFormatException e) when (!IsAssembly(file))
				{
					// Not a .NET assembly, such as a native library a plugin uses, as the folder's catalog skipped before (#1216).
					// A .NET assembly that can't be loaded, for example one built for another processor, is warned about below.
					catalog?.Dispose();
					Log.Default.Write(LogSeverityType.Debug, e, $"A file in the plugin folder is not a .NET assembly, so it is skipped. File={file}");
				}
				catch (Exception e)
				{
					catalog?.Dispose();
					Log.Default.Write(LogSeverityType.Warning, e, $"A plugin assembly could not be loaded, so it is skipped. File={file}");
				}
			}

			using var aggregate = new AggregateCatalog(catalogs);
			using var container = new CompositionContainer(aggregate);
			var exports = new List<T>();

			try
			{
				foreach (Lazy<T> export in container.GetExports<T>())
				{
					// Each part on its own, so a plugin whose constructor throws, for example for want of a license file, is the only one skipped.
					try
					{
						exports.Add(export.Value);
					}
					catch (Exception e)
					{
						Log.Default.Write(LogSeverityType.Warning, e, $"A plugin could not be created, so it is skipped. Folder={folder}, Export={typeof(T).Name}");
					}
				}
			}
			catch (Exception e)
			{
				Log.Default.Write(LogSeverityType.Warning, e, $"The plugins could not be composed, so none is used. Folder={folder}, Export={typeof(T).Name}");
				return Array.Empty<T>();
			}

			return exports;
		}

		private static bool IsAssembly(string file)
		{
			try
			{
				_ = System.Reflection.AssemblyName.GetAssemblyName(file);
				return true;
			}
			catch (Exception e) when (e is BadImageFormatException or IOException or ArgumentException)
			{
				return false;
			}
		}

		private IPlugin Create(string sourceFilePath, LogSeverityType foundSeverity, LogSeverityType notFoundSeverity)
		{
			// Once, even when two threads ask at the same time, as the Open As dialog may.
			LazyInitializer.EnsureInitialized(ref _plugins, ref _pluginsLock, LoadPlugins);

			IPlugin compatiblePlugin = null;

			foreach (IPlugin plugin in _plugins)
			{
				Log.Default.Write(LogSeverityType.Debug, $"Checking if plugin is able to parse the file. Plugin={plugin.Name}, SourceFilePath={sourceFilePath}");

				if (plugin.CheckCompatibility(sourceFilePath))
				{
					compatiblePlugin = plugin;
					Log.Default.Write(foundSeverity, $"A compatible plugin has been found for the input file. Plugin={plugin.Name}, SourceFilePath={sourceFilePath}");
					break;
				}
			}

			if (compatiblePlugin is DefaultPlugin)
			{
				Log.Default.Write(notFoundSeverity, $"A compatible plugin could not be found for the input file. SourceFilePath={sourceFilePath}");
			}

			return compatiblePlugin;
		}

		private static IReadOnlyList<IPlugin> LoadPlugins()
		{
			var plugins = new List<IPlugin>();

			if (Directory.Exists(_pluginDirectoryPath))
			{
				// Note: If a plugin fails to load at run-time
				// ... perform a `Rebuild All` in Visual Studio
				// ... to ensure that the plugin cache is up-to-date
				plugins.AddRange(LoadExports<IPlugin>(_pluginDirectoryPath));

				if (plugins.Count == 0)
				{
					Log.Default.Write(
						LogSeverityType.Warning,
						"Unable to find any plugins.  The plugin directory is either empty, or there is a 32/64 bit mismatch.");
				}
			}
			else
			{
				Log.Default.Write(
					LogSeverityType.Warning,
					"Plugin directory is missing.");
			}

			plugins.Add(new TsvPlugin());
			plugins.Add(new DefaultPlugin());

			return plugins;
		}
	}
}
