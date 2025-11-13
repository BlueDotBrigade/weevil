using System;
using System.Collections.Immutable;
using System.Linq;
using System.Windows;
using BlueDotBrigade.Weevil;
using BlueDotBrigade.Weevil.Analysis;
using BlueDotBrigade.Weevil.Data;
using BlueDotBrigade.Weevil.Gui.Analysis;
using BlueDotBrigade.Weevil.Gui.IO;
using BlueDotBrigade.Weevil.Gui.Navigation;
using Microsoft.Win32;

internal class DialogBoxService : IDialogBoxService
{
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

	public void ShowDashboard(Version weevilVersion, IEngine engine, ImmutableArray<IInsight> insights)
	{
		var dialog = new DashboardDialog(weevilVersion, engine)
		{
			Insights = insights.ToArray(),
		};

		dialog.Show();
	}

	public void ShowGraph(ImmutableArray<IRecord> records, string selectedPattern)
	{
		var dialog = new GraphDialog(records, selectedPattern);
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
}