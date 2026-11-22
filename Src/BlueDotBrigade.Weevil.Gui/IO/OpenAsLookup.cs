namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Windows.Dialogs.Storage;
	using BlueDotBrigade.Weevil.Windows.IO;

	/// <summary>
	/// What the Open As dialog reads about the log selected, without opening or extracting it: its sections and regions (#1212, #1213),
	/// and its plugin's options provider (#1216). Runs off the UI thread.
	/// </summary>
	internal static class OpenAsLookup
	{
		/// <summary>
		/// How much of a log in a zip is read to find its plugin, which tells its logs by their start (#1216).
		/// </summary>
		internal const int PluginCheckBytes = 64 * 1024;

		/// <summary>
		/// Lists the sections and regions of the log selected, from its sidecar.
		/// </summary>
		/// <remarks>
		/// For a log in a zip, the sidecar is the one Weevil kept beside the copy of an earlier open, which holds the analyst's regions (#1253),
		/// or else the one beside the log in the zip, read through a temporary copy.
		/// </remarks>
		internal static SidecarParts ReadParts(IContainerReader reader, string copiesFolder, BrowserItem file)
		{
			if (!file.Location.IsInArchive)
			{
				return ReadParts(file.Location.FileSystemPath);
			}

			if (GetSafeName(file.Location) is not { } name)
			{
				return SidecarParts.NoSidecar;
			}

			var copy = Path.Combine(copiesFolder, reader.GetCopyKey(file.Location), name);

			if (File.Exists($"{copy}.xml"))
			{
				return ReadParts(copy);
			}

			Location sidecar = ContainerReader.GetSibling(file.Location, $"{name}.xml");

			if (!reader.Exists(sidecar))
			{
				return SidecarParts.NoSidecar;
			}

			DirectoryInfo folder = Directory.CreateTempSubdirectory("Weevil.OpenAs.");

			try
			{
				var log = Path.Combine(folder.FullName, name);

				using (Stream source = reader.OpenRead(sidecar))
				using (FileStream target = File.Create($"{log}.xml"))
				{
					source.CopyTo(target);
				}

				return ReadParts(log);
			}
			finally
			{
				TryDelete(folder);
			}
		}

		/// <summary>
		/// Finds the options provider of the plugin of the log selected, when it has one (#1216).
		/// </summary>
		/// <remarks>
		/// Without providers, as for most analysts, no log is read to find its plugin. A plugin tells its logs by their start, so for a log in a zip
		/// only its first <see cref="PluginCheckBytes"/> are read from the zip, into a temporary file of the same name: the log is not extracted.
		/// A log stored online only uses Weevil's options, since reading any of it would download it.
		/// </remarks>
		internal static IOpenAsOptionsProvider FindOptionsProvider(PluginFactory plugins, IReadOnlyList<IOpenAsOptionsProvider> providers, IContainerReader reader, BrowserItem file)
		{
			if (providers.Count == 0 || file.IsOnlineOnly)
			{
				return null;
			}

			var pluginName = file.Location.IsInArchive
				? FindZippedLogsPlugin(plugins, reader, file)
				: plugins.Find(file.Location.FileSystemPath).Name;

			return pluginName is null
				? null
				: providers.FirstOrDefault(provider => string.Equals(provider.PluginName, pluginName, StringComparison.Ordinal));
		}

		/// <summary>
		/// The name of the plugin of a log in a zip, found from the log's first <see cref="PluginCheckBytes"/>, or <see langword="null"/> for a name a file can't have.
		/// </summary>
		private static string FindZippedLogsPlugin(PluginFactory plugins, IContainerReader reader, BrowserItem file)
		{
			if (GetSafeName(file.Location) is not { } name)
			{
				return null;
			}

			DirectoryInfo folder = Directory.CreateTempSubdirectory("Weevil.OpenAs.");

			try
			{
				var start = Path.Combine(folder.FullName, name);

				using (Stream source = reader.OpenRead(file.Location))
				using (FileStream target = File.Create(start))
				{
					var buffer = new byte[PluginCheckBytes];
					var length = 0;
					int read;

					while (length < buffer.Length && (read = source.Read(buffer, length, buffer.Length - length)) > 0)
					{
						length += read;
					}

					target.Write(buffer, 0, length);
				}

				return plugins.Find(start).Name;
			}
			finally
			{
				TryDelete(folder);
			}
		}

		/// <summary>
		/// Lists the sections and regions of a log on disk, from the sidecar beside it.
		/// </summary>
		private static SidecarParts ReadParts(string logPath)
		{
			return File.Exists($"{logPath}.xml")
				? new SidecarParts(true, Engine.ReadParts(logPath))
				: SidecarParts.NoSidecar;
		}

		/// <summary>
		/// The name of a file in a zip, or <see langword="null"/> when it would leave a folder it is put in, such as <c>C:other.log</c>,
		/// which extracting it refuses too.
		/// </summary>
		private static string GetSafeName(Location location)
		{
			var name = location.Name;

			return string.IsNullOrEmpty(name) || name.IndexOfAny(new[] { ':', '\\', '/' }) >= 0 || Path.IsPathRooted(name)
				? null
				: name;
		}

		/// <summary>
		/// Deletes a temporary folder of Open As. What it read is kept: only the temporary copy is left behind, for example while a scanner holds it.
		/// </summary>
		private static void TryDelete(DirectoryInfo folder)
		{
			try
			{
				folder.Delete(recursive: true);
			}
			catch (Exception e) when (e is IOException or UnauthorizedAccessException)
			{
				Log.Default.Write(LogSeverityType.Warning, e, $"Open As could not delete a temporary copy. Folder={folder.FullName}");
			}
		}
	}
}
