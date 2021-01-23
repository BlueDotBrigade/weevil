namespace BlueDotBrigade.Weevil
{
	using System.Collections.Generic;
	using System.ComponentModel.Composition.Hosting;
	using System.IO;
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
			IEnumerable<IPlugin> plugins = LoadPlugins();
			IPlugin compatiblePlugin = null;

			foreach (IPlugin plugin in plugins)
			{
				Log.Default.Write(LogSeverityType.Debug, $"Looking for an appropriate plugin for the input file. Plugin={plugin.Name}, SourceFilePath={sourceFilePath}");

				if (plugin.CheckCompatibility(sourceFilePath))
				{
					compatiblePlugin = plugin;
					Log.Default.Write(LogSeverityType.Information, $"A compatible plugin has been found for the input file. Plugin={plugin.Name}, SourceFilePath={sourceFilePath}");
					break;
				}
			}

			if (compatiblePlugin is null)
			{
				Log.Default.Write(LogSeverityType.Warning, $"A compatible plugin could not be found for the input file. SourceFilePath={sourceFilePath}");
				compatiblePlugin = new TheDefaultPlugin();
			}

			return compatiblePlugin;
		}

		private static IEnumerable<IPlugin> LoadPlugins()
		{
			var plugins = new List<IPlugin>();

			if (Directory.Exists(_pluginDirectoryPath))
			{
				using (var catalog = new DirectoryCatalog(_pluginDirectoryPath, "*.dll"))
				{
					using (var container = new CompositionContainer(catalog))
					{
						foreach (IPlugin plugin in container.GetExportedValues<IPlugin>())
						{
							plugins.Add(plugin);
						}
					}
				}

				if (plugins.Count == 0)
				{
					Log.Default.Write(
						LogSeverityType.Warning,
						"Unable to load any plugins.  The plugin directory is either empty, or there is a 32/64 bit mismatch.");
				}
			}
			else
			{
				Log.Default.Write(
					LogSeverityType.Warning,
					"Plugin directory is missing.");
			}

			return plugins;
		}
	}
}
