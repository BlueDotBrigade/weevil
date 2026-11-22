using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using BlueDotBrigade.Weevil;
using BlueDotBrigade.Weevil.Analysis;
using BlueDotBrigade.Weevil.Data;
using BlueDotBrigade.Weevil.Diagnostics;
using BlueDotBrigade.Weevil.Gui;
using BlueDotBrigade.Weevil.Gui.Analysis;
using BlueDotBrigade.Weevil.Gui.IO;
using BlueDotBrigade.Weevil.Gui.Navigation;
using BlueDotBrigade.Weevil.Windows.Dialogs.Storage;
using BlueDotBrigade.Weevil.Windows.IO;
using Microsoft.Win32;
using StorageMode = BlueDotBrigade.Weevil.IO.StorageMode;

/// <summary>
/// Manages application-wide state for graph windows.
/// </summary>
internal static class GraphWindowCounter
{
	private static int _counter;

	/// <summary>
	/// Gets the next sequential graph window number in a thread-safe manner.
	/// </summary>
	/// <returns>The next window number (1, 2, 3, etc.)</returns>
	public static int GetNext()
	{
		return Interlocked.Increment(ref _counter);
	}
}

internal class DialogBoxService : IDialogBoxService
{
	/// <summary>
	/// The folder, inside Weevil's plugin folder, of the WPF assemblies only Weevil's window loads, such as a plugin's Open As options (#1216).
	/// </summary>
	/// <remarks>
	/// Core, the CLI and PowerShell load only the assemblies directly in the plugin folder, so they never load WPF.
	/// </remarks>
	internal const string WpfPluginFolder = "Wpf";

	/// <summary>
	/// How much of a log in a zip is read to find its plugin, which tells its logs by their start (#1216).
	/// </summary>
	private const int PluginCheckBytes = 64 * 1024;

	// Rolled logs, such as app.log.1, are logs too. Weevil's sidecar and index are not (decision 9 of the #1157 plan).
	private static readonly FileTypeFilter LogFiles = new FileTypeFilter(
		"Log files (*.log, *.log.1, *.txt, *.tsv, *.csv)",
		new[] { "*.log", "*.log.*", "*.txt", "*.tsv", "*.csv" },
		new[] { "*.xml", "*.idx" });

	private DashboardDialog _activeDashboard;

	/// <summary>
	/// Used to display dialog boxes (e.g. error messages) to the user.
	/// </summary>
	public DialogBoxService()
	{
		// nothing to do
	}

	public bool TryShowOpenAs(Action<string> checkBeforeOpening, out OpenAsChoice choice)
	{
		// Read on the UI thread once the dialog exists; the parts are listed off it.
		IContainerReader reader = null;
		var options = new OpenAsOptionsViewModel(OpenAsAdvice.Read, file => ReadParts(reader, ExtractedLog.DefaultCopiesFolder, file));

		// Weevil's options, unless the selected log's plugin shows its own (#1216). The providers are loaded once, off the UI thread, at the first selection.
		var plugins = new PluginFactory();
		var providers = new Lazy<IReadOnlyList<IOpenAsOptionsProvider>>(() => PluginFactory.LoadExports<IOpenAsOptionsProvider>(Path.Combine(PluginFactory.DirectoryPath, WpfPluginFolder)));
		using var host = new OpenAsOptionsHost(options, new OpenAsOptionsView { DataContext = options }, file => FindOptionsProvider(plugins, providers.Value, reader, ExtractedLog.DefaultCopiesFolder, file));
		var dialog = new BlueDotBrigade.Weevil.Windows.Dialogs.OpenAsDialog
		{
			Owner = Application.Current?.MainWindow,
			Title = "Open",
			FileTypes = new[] { LogFiles, FileTypeFilter.AllFiles },
			CompanionSuffixes = ExtractedLog.CompanionSuffixes,
			CopiesFolder = ExtractedLog.DefaultCopiesFolder,
			CheckBeforeOpening = checkBeforeOpening,
			OptionsContent = host.View,
			CanOpen = false,
		};

		reader = dialog.Browser.Reader;

		// The dialog has no theme of its own: Weevil's application-wide styles restyle its controls, and its window takes Weevil's colours here.
		dialog.SetResourceReference(Control.BackgroundProperty, "MaterialDesignPaper");
		dialog.SetResourceReference(Control.ForegroundProperty, "MaterialDesignBody");
		dialog.SetResourceReference(Control.FontFamilyProperty, "MaterialDesignFont");
		dialog.SetResourceReference(Control.FontSizeProperty, "ApplicationFontSize");

		dialog.SelectionChanged += async (_, item) => await host.SelectAsync(item);
		host.Changed += (_, _) =>
		{
			dialog.OptionsContent = host.View;
			dialog.CanOpen = host.CanOpen;
		};

		var isAccepted = dialog.ShowDialog() == true;

		// Weevil's advice is logged only when its options were shown.
		choice = isAccepted
			? new OpenAsChoice(
				dialog.Result.LocalPath,
				dialog.Result.ExtractedFolder,
				host.Options.StorageMode,
				ReferenceEquals(host.Options, options) ? options.Advice : null,
				dialog.Result.Item.Location.IsInArchive ? dialog.Result.Item.Location.ToString() : null,
				host.Options.Part,
				host.Options.Context)
			: null;

		return isAccepted;
	}

	/// <summary>
	/// Finds the options provider of the plugin of the log selected in the Open As dialog, when it has one (#1216). Runs off the UI thread.
	/// </summary>
	/// <remarks>
	/// Without providers, as for most analysts, no log is read to find its plugin. A plugin tells its logs by their start, so for a log in a zip
	/// only its first <see cref="PluginCheckBytes"/> are read from the zip, unless an earlier open kept a copy (#1253): the log is not extracted.
	/// A log stored online only uses Weevil's options, since reading any of it would download it.
	/// </remarks>
	private static IOpenAsOptionsProvider FindOptionsProvider(PluginFactory plugins, IReadOnlyList<IOpenAsOptionsProvider> providers, IContainerReader reader, string copiesFolder, BrowserItem file)
	{
		if (providers.Count == 0 || file.IsOnlineOnly)
		{
			return null;
		}

		var pluginName = file.Location.IsInArchive
			? FindZippedLogsPlugin(plugins, reader, copiesFolder, file)
			: plugins.Find(file.Location.FileSystemPath).Name;

		return providers.FirstOrDefault(provider => string.Equals(provider.PluginName, pluginName, StringComparison.Ordinal));
	}

	/// <summary>
	/// The name of the plugin of a log in a zip: from the copy an earlier open kept, or else from the log's first <see cref="PluginCheckBytes"/>,
	/// read into a temporary file of the same name.
	/// </summary>
	private static string FindZippedLogsPlugin(PluginFactory plugins, IContainerReader reader, string copiesFolder, BrowserItem file)
	{
		var copy = Path.Combine(copiesFolder, reader.GetCopyKey(file.Location), file.Location.Name);

		if (File.Exists(copy))
		{
			return plugins.Find(copy).Name;
		}

		DirectoryInfo folder = Directory.CreateTempSubdirectory("Weevil.OpenAs.");

		try
		{
			var start = Path.Combine(folder.FullName, file.Location.Name);

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
	/// Lists the sections and regions of the log selected in the Open As dialog (#1212, #1213), from its sidecar.
	/// </summary>
	/// <remarks>
	/// For a log in a zip, the sidecar is the one Weevil kept beside the copy of an earlier open, which holds the analyst's regions (#1253),
	/// or else the one beside the log in the zip, read through a temporary copy. Runs off the UI thread.
	/// </remarks>
	private static SidecarParts ReadParts(IContainerReader reader, string copiesFolder, BrowserItem file)
	{
		if (!file.Location.IsInArchive)
		{
			return ReadParts(file.Location.FileSystemPath);
		}

		var copy = Path.Combine(copiesFolder, reader.GetCopyKey(file.Location), file.Location.Name);

		if (File.Exists($"{copy}.xml"))
		{
			return ReadParts(copy);
		}

		Location sidecar = ContainerReader.GetSibling(file.Location, $"{file.Location.Name}.xml");

		if (!reader.Exists(sidecar))
		{
			return SidecarParts.NoSidecar;
		}

		DirectoryInfo folder = Directory.CreateTempSubdirectory("Weevil.OpenAs.");

		try
		{
			var log = Path.Combine(folder.FullName, file.Location.Name);

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

	public string ShowSaveFile(string targetDirectory, string targetFileName)
	{
		var dialog = new SaveFileDialog
		{
			InitialDirectory = targetDirectory,
			FileName = targetFileName
		};

		if (dialog.ShowDialog() == true)
		{
			return dialog.FileName;
		}

		return string.Empty;
	}

	public void ShowDashboard(Version weevilVersion, IEngine engine, ImmutableArray<IInsight> insights, IBulletinMediator bulletinMediator)
	{
		// If an active dashboard exists and is loaded, activate it instead of creating a new one
		if (_activeDashboard != null && _activeDashboard.IsLoaded)
		{
			// If the dashboard is minimized, restore it to normal state
			if (_activeDashboard.WindowState == WindowState.Minimized)
			{
				_activeDashboard.WindowState = WindowState.Normal;
			}

			// Bring the dashboard to the foreground and activate it
			_activeDashboard.Activate();

			return;
		}

		// Create a new dashboard if no active dashboard exists
		var dialog = new DashboardDialog(weevilVersion, engine, bulletinMediator)
		{
			Insights = insights.ToArray(),
			Owner = Application.Current?.MainWindow
		};

		// Track the active dashboard
		_activeDashboard = dialog;
		
		// Clean up reference when dashboard is closed
		dialog.Closed += (sender, args) =>
		{
			if (_activeDashboard == dialog)
			{
				_activeDashboard = null;
			}
		};

		dialog.Show();
	}

	public void CloseDashboard()
	{
		_activeDashboard?.Close();
	}

	public void ShowMessage(string message, string caption)
	{
		MessageBox.Show(message, caption);
	}

	public bool ShowQuestion(string message, string caption)
	{
		return MessageBox.Show(message, caption, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
	}

	public void ShowGraph(RecordList records, string selectedPattern, string sourceFilePath)
	{
		var windowNumber = GraphWindowCounter.GetNext();
		var windowTitle = $"Graph{windowNumber}";
		var dialog = new GraphDialog(records, selectedPattern, windowTitle, sourceFilePath);
		dialog.Show();
	}

	public string ShowUserPrompt(string title, string userPrompt)
	{
		return ShowUserPrompt(title, userPrompt, string.Empty);
	}

	public string ShowUserPrompt(string title, string userPrompt, string defaultValue)
	{
		var dialog = new UserPromptDialog()
		{
			Title = title,
			UserPrompt = userPrompt,
			UserInput = defaultValue ?? string.Empty
		};

		if (dialog.ShowDialog() == true)
		{
			return dialog.UserInput;
		}
		return string.Empty;
	}

	public bool TryShowUserPrompt(string title, string userPrompt, string validationPattern, string validationError, out string userValue)
	{
		userValue = string.Empty;
		var wasSuccessful = false;

		var dialog = new UserPromptDialog(validationPattern, validationError)
		{
			Title = title,
			UserPrompt = userPrompt,
		};

		if (dialog.ShowDialog() == true)
		{
			userValue = dialog.UserInput;
			wasSuccessful = true;
		}

		return wasSuccessful;
	}


	public bool TryShowOpenAs(OpenAsAdvice advice, out StorageMode storageMode)
	{
		var dialog = new OpenAsDialog(advice);
		var isAccepted = dialog.ShowDialog() == true;

		storageMode = dialog.StorageMode;

		return isAccepted;
	}

	public bool TrySelectCompressedFile(string[] fileNames, out string selectedFileName)
	{
		var viewModel = new SelectFileViewModel(fileNames);
		new SelectFileView(viewModel).ShowDialog();

		selectedFileName = viewModel.SelectedFilename;
		return !string.IsNullOrEmpty(selectedFileName);
	}

	public bool TryShowGoTo(string defaultValue, out string userValue)
	{
		userValue = string.Empty;
		var wasSuccessful = false;

		var dialog = new GoToDialog()
		{
			Title = "Go To",
			UserPrompt = "Timestamp or line #:",
			UserInput = defaultValue ?? string.Empty
		};

		if (dialog.ShowDialog() == true)
		{
			userValue = dialog.UserInput;
			wasSuccessful = true;
		}
		return wasSuccessful;
	}

	public bool TryShowFind(
		string defaultValue, 
		out bool isCaseSensitive, 
		out bool findNext, 
		out bool useRegex, 
		out string findText,
		out bool searchElapsedTime,
		out int? minElapsedMs,
		out int? maxElapsedMs,
		out bool searchComments)
	{
		var wasSuccessful = false;

		isCaseSensitive = false;
		findText = String.Empty;
		findNext = true;
		useRegex = false;
		searchElapsedTime = false;
		searchComments = false;
		minElapsedMs = null;
		maxElapsedMs = null;

		var dialog = new FindDialog(defaultValue);

		if (dialog.ShowDialog() == true)
		{
			isCaseSensitive = dialog.IsCaseSensitive;
			findText = dialog.UserInput;
			findNext = dialog.FindNext;
			useRegex = dialog.IsRegexMode;
			searchElapsedTime = dialog.SearchElapsedTime;
			searchComments = dialog.SearchComments;

			// Parse elapsed time values
			if (searchElapsedTime)
			{
				if (!string.IsNullOrWhiteSpace(dialog.MinElapsedTimeMs) && int.TryParse(dialog.MinElapsedTimeMs, out var minMs))
				{
					minElapsedMs = minMs;
				}

				if (!string.IsNullOrWhiteSpace(dialog.MaxElapsedTimeMs) && int.TryParse(dialog.MaxElapsedTimeMs, out var maxMs))
				{
					maxElapsedMs = maxMs;
				}
			}

			wasSuccessful = true;
		}

		return wasSuccessful;
	}

	public bool TryGetExpressions(string defaultRegex, string recordsDescription, out string regularExpression)
	{
		regularExpression = string.Empty;
		var wasSuccessful = false;

		var dialog = new AnalysisDialog()
		{
			RegularExpression = defaultRegex ?? string.Empty,
			RecordsDescription = recordsDescription ?? "All"
		};

		if (dialog.ShowDialog() == true)
		{
			regularExpression = dialog.RegularExpression;
			wasSuccessful = true;
		}

		return wasSuccessful;
	}

	public bool TryGetThreshold(
		string defaultRegex,
		string recordsDescription,
		string defaultThreshold,
		string defaultComparison,
		out string regularExpression,
		out string threshold,
		out string comparison)
	{
		regularExpression = string.Empty;
		threshold = defaultThreshold ?? "0";
		comparison = defaultComparison ?? ">";
		var wasSuccessful = false;

		var dialog = new ThresholdDialog
		{
			RegularExpression = defaultRegex ?? string.Empty,
			RecordsDescription = recordsDescription ?? "All",
			ThresholdValue = threshold,
			Comparison = comparison
		};

		if (dialog.ShowDialog() == true)
		{
			regularExpression = dialog.RegularExpression;
			threshold = dialog.ThresholdValue;
			comparison = dialog.Comparison;
			wasSuccessful = true;
		}

		return wasSuccessful;
	}
}
