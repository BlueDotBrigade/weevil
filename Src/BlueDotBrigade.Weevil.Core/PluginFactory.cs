namespace BlueDotBrigade.Weevil
{
	using System.Collections.Generic;
	using System.ComponentModel.Composition.Hosting;
	using System.IO;
	using System.Linq;
	using BlueDotBrigade.Weevil.Diagnostics;

	public class PluginFactory
	{
		private const string PluginDirectory = @"Plugins";
		private static readonly string _pluginDirectoryPath;

		static PluginFactory()
		{
			var executingAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
			var executingAssemblyDirectory = Path.GetDirectoryName(executingAssemblyPath);
			_pluginDirectoryPath = Path.Combine(executingAssemblyDirectory, PluginDirectory);
		}

		public IPlugin Create(string sourceFilePath)
		{
			return Create(sourceFilePath, LogSeverityType.Information, LogSeverityType.Warning);
		}

		/// <summary>
		/// Returns the plugin that reads the log, as <see cref="Create(string)"/> does, but writes to Weevil's log only at Debug,
		/// for a caller that asks often, such as the Open As dialog each time the analyst selects a log (#1216).
		/// </summary>
		/// <remarks>
		/// A plugin may read the log to tell whether it is one of its logs, so call it off the UI thread.
		/// </remarks>
		public IPlugin Find(string sourceFilePath)
		{
			return Create(sourceFilePath, LogSeverityType.Debug, LogSeverityType.Debug);
		}

		private static IPlugin Create(string sourceFilePath, LogSeverityType foundSeverity, LogSeverityType notFoundSeverity)
		{
			IList<IPlugin> plugins = LoadThirdPartyPlugins();
			plugins.Add(new TsvPlugin());
			plugins.Add(new DefaultPlugin());

			IPlugin compatiblePlugin = null;

			foreach (IPlugin plugin in plugins)
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

		private static IList<IPlugin> LoadThirdPartyPlugins()
		{
			var plugins = new List<IPlugin>();

			if (Directory.Exists(_pluginDirectoryPath))
			{
				using (var catalog = new DirectoryCatalog(_pluginDirectoryPath, "*.dll"))
				{
					using (var container = new CompositionContainer(catalog))
					{
						// Note: If a plugin fails to load at run-time
						// ... perform a `Rebuild All` in Visual Studio
						// ... to ensure that the plugin cache is up-to-date
						plugins.AddRange(container.GetExportedValues<IPlugin>());
					}
				}

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

			return plugins.ToList();
		}
	}
}