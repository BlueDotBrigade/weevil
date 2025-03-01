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
					Log.Default.Write(LogSeverityType.Information, $"A compatible plugin has been found for the input file. Plugin={plugin.Name}, SourceFilePath={sourceFilePath}");
					break;
				}
			}

			if (compatiblePlugin is DefaultPlugin)
			{
				Log.Default.Write(LogSeverityType.Warning, $"A compatible plugin could not be found for the input file. SourceFilePath={sourceFilePath}");
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