namespace BlueDotBrigade.Weevil.Configuration.Sidecar
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.IO;
	using Data;
	using Diagnostics;
	using IO;
	using Navigation;

	public class SidecarManager : IPersist
	{
		private const string MetadataFileExtension = "xml";

		private readonly IFile _file;
		private readonly string _sidecarFilePath;

		public SidecarManager(string logFilePath)
		{
			_sidecarFilePath = string.IsNullOrEmpty(logFilePath)
				 ? throw new ArgumentNullException(nameof(logFilePath))
				 : $"{logFilePath}.{MetadataFileExtension}";
			_file = new IO.File();
		}

		/// <summary>
		/// Determines if the sidecar file path is in a temporary directory.
		/// </summary>
		private bool IsInTemporaryDirectory()
		{
			try
			{
				var tempPath = Path.GetTempPath();
				var sidecarDirectory = Path.GetDirectoryName(_sidecarFilePath);
				
				// Check if the sidecar path starts with the temp path
				return sidecarDirectory?.StartsWith(tempPath, StringComparison.OrdinalIgnoreCase) ?? false;
			}
			catch
			{
				// If we can't determine, assume it's not in temp directory
				return false;
			}
		}

		public void Load(
			ImmutableArray<IRecord> allRecords,
			out ContextDictionary context,
			out string sourceFileRemarks,
			out List<string> inclusiveFilterHistory,
			out List<string> exclusiveFilterHistory,
			out List<Section> tableOfContents,
			out List<Region> regions,
			out List<Bookmark> bookmarks)
		{
			context = new ContextDictionary();
			sourceFileRemarks = string.Empty;
			inclusiveFilterHistory = new List<string>();
			exclusiveFilterHistory = new List<string>();
			tableOfContents = new List<Section>();
			regions = new List<Region>();
			bookmarks = new List<Bookmark>();

			if (_file.Exists(_sidecarFilePath))
			{
				// The following local variables are required because
				// ... the C# compiler does not support referencing `out` parameters from a delegate.
				List<string> inclusiveHistory = inclusiveFilterHistory;
				List<string> exclusiveHistory = exclusiveFilterHistory;
				ContextDictionary contextProperties = context;
				string actualSourceFileRemarks = sourceFileRemarks;
				List<Section> toc = tableOfContents;
				List<Region> regionsRef = regions;
				List<Bookmark> bookmarkRef = bookmarks;

				var loaders = new List<IExecute>
				{
					Executor.Create(
						new v2.SidecarLoader(_sidecarFilePath),
						x => x.Load(),
						x => x.Load(
							allRecords, 
							contextProperties,
							out actualSourceFileRemarks, 
							inclusiveHistory, 
							exclusiveHistory, 
							toc,
							regionsRef,
							bookmarkRef)),

					Executor.Create(
						new v1.LogMetadataLoader(_sidecarFilePath),
						x => x.Load(),
						x => x.Apply(allRecords)),
				};

				var sidecarLoaded = false;

				foreach (IExecute loader in loaders)
				{
					if (loader.CanExecute())
					{
						sidecarLoaded = true;
						loader.Execute();
						break;
					}
				}

				if (sidecarLoaded == true)
				{
					sourceFileRemarks = actualSourceFileRemarks; // HACK: Remove this hack to get around passing variables to anonymous methods.
				}
				else
				{
					var message = "This file's sidecar is not compatible with this version of the application.";
					Log.Default.Write(
						LogSeverityType.Warning,
						message);
					throw new NotSupportedException(message);
				}
			}
			else
			{
				Log.Default.Write(
					LogSeverityType.Warning,
					"The source file does not appear to have a sidecar.");
			}
		}

		public void Save(SidecarData data, bool deleteBackup)
		{
			// Skip save operation if the file is from a compressed archive (in temp directory)
			if (IsInTemporaryDirectory())
			{
				Log.Default.Write(
					LogSeverityType.Information,
					$"Skipping sidecar save for file from compressed archive. File={_sidecarFilePath}");
				return;
			}

			var backupFilePath = $"{_sidecarFilePath}~";

			Log.Default.Write(LogSeverityType.Debug, $"Sidecar data is being saved... File={_sidecarFilePath}");

			BackupPreviousSidecar(backupFilePath);

			new v2.SidecarLoader(_sidecarFilePath).Save(data, deleteBackup);

			Log.Default.Write(LogSeverityType.Information,
					$"Sidecar data has been saved. File={_sidecarFilePath}");
		}

		private void BackupPreviousSidecar(string backupFilePath)
		{
			if (_file.Exists(_sidecarFilePath))
			{
				if (_file.Exists(backupFilePath))
				{
					_file.Delete(backupFilePath);
				}

				_file.Copy(_sidecarFilePath, backupFilePath);
			}
		}

		public long GetByteOffsetOrDefault(int lineNumber)
		{
			long offset = 0;

			if (_file.Exists(_sidecarFilePath))
			{
				var loader = new v2.SidecarLoader(_sidecarFilePath);
				if (loader.Load())
				{
					offset = loader.GetByteOffsetOrDefault(lineNumber);
				}
			}

			return offset;
		}
	}
}