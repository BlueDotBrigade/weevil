using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Windows;
using BlueDotBrigade.Weevil;
using BlueDotBrigade.Weevil.Analysis;
using BlueDotBrigade.Weevil.Data;
using BlueDotBrigade.Weevil.Gui;
using BlueDotBrigade.Weevil.Gui.Analysis;
using BlueDotBrigade.Weevil.Gui.IO;
using BlueDotBrigade.Weevil.Gui.Navigation;
using Microsoft.Win32;

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
	private DashboardDialog _activeDashboard;

	/// <summary>
	/// Used to display dialog boxes (e.g. error messages) to the user.
	/// </summary>
	public DialogBoxService()
	{
		// nothing to do
	}

	public string ShowOpenFile(string compatibleFileExtensions)
	{
		var dialog = new OpenFileDialog
		{
			Filter = compatibleFileExtensions
		};

		if (dialog.ShowDialog() == true)
		{
			return dialog.FileName;
		}

		return string.Empty;
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

	public void ShowGraph(ImmutableArray<IRecord> records, string selectedPattern, string sourceFilePath)
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
		out int? maxElapsedMs)
	{
		var wasSuccessful = false;

		isCaseSensitive = false;
		findText = String.Empty;
		findNext = true;
		useRegex = false;
		searchElapsedTime = false;
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

	public bool TryShowAnalysisDialog(string defaultRegex, string recordsDescription, out string regularExpression)
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
}
