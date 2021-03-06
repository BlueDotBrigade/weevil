﻿namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System;
	using System.Collections.Immutable;
	using System.Linq;
	using System.Windows;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Gui.Analysis;
	using Microsoft.Win32;

	internal class DialogBoxService : IDialogBoxService
	{
		private readonly Window _parentWindow;

		/// <summary>
		/// Used to display dialog boxes (e.g. error messages) to the user.
		/// </summary>
		/// <param name="parentWindow">Represents the <see cref="Window"/> to center the dialog box on.</param>
		/// <example>
		/// // Retrieves the root level .
		/// Window applicationWindow = Application.Current.MainWindow
		/// 
		/// // Retrieves the immediate parent.
		/// Window parentWindow = Window.GetWindow(this);
		/// </example>
		public DialogBoxService(Window parentWindow)
		{
			_parentWindow = parentWindow;
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

		public void ShowDashboard(ImmutableArray<IInsight> insights, IEngine engine)
		{
			var dialog = new DashboardDialog(engine)
			{
				Insights = insights.ToArray(),
			};

			dialog.Show();
	}

		public string ShowUserPrompt(string title, string userPrompt)
		{
			return ShowUserPrompt(title, userPrompt, string.Empty);
		}

		public string ShowUserPrompt(string title, string userPrompt, string defaultValue)
		{
			var dialog = new UserPromptDialog
			{
				Owner = _parentWindow,
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

		public bool TryShowFind(out bool findNext, out string findText)
		{
			var wasSuccessful = false;

			findText = String.Empty;
			findNext = true;

			var dialog = new FindDialog()
			{
				Owner = _parentWindow,
			};

			if (dialog.ShowDialog() == true)
			{
				wasSuccessful = true;
				findText = dialog.UserInput;
				findNext = dialog.FindNext;
			}

			return wasSuccessful;
		}
	}
}
