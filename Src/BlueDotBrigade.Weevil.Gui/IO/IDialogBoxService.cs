namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.IO;

	internal interface IDialogBoxService : IUserDialog
	{
		/// <summary>
		/// Shows the Open As dialog, so the user finds a log and chooses RAM or Disk in one window (#1215).
		/// </summary>
		/// <param name="choice">The log chosen and its mode, or <see langword="null"/> when the user cancelled.</param>
		/// <returns><see langword="false"/> when the user cancelled: nothing opens, and nothing was extracted.</returns>
		bool TryShowOpenAs(out OpenAsChoice choice);

		/// <summary>
		/// Shows Weevil's Open As window for a log already chosen, so the user chooses RAM or Disk for it (#1028).
		/// </summary>
		/// <param name="advice">The log, the mode suggested for it, and the warnings.</param>
		/// <param name="storageMode">The mode chosen.</param>
		/// <returns><see langword="false"/> when the user cancelled: nothing opens.</returns>
		bool TryShowOpenAs(OpenAsAdvice advice, out StorageMode storageMode);

		/// <summary>
		/// Lets the user choose one of the files extracted from a zip.
		/// </summary>
		/// <returns><see langword="false"/> when the user chose no file.</returns>
		bool TrySelectCompressedFile(string[] fileNames, out string selectedFileName);

		string ShowSaveFile(string targetDirectory, string targetFileName);

		void ShowMessage(string message, string caption);

		/// <summary>
		/// Asks the user a question that has a Yes or a No answer.
		/// </summary>
		/// <returns><see langword="true"/> when the user answered Yes.</returns>
		bool ShowQuestion(string message, string caption);

		string ShowUserPrompt(string title, string userPrompt);

		bool TryShowUserPrompt(string title, string userPrompt, string validationPattern, string validationError, out string userValue);

		void ShowDashboard(Version weevilVersion, IEngine engine, ImmutableArray<IInsight> insights, IBulletinMediator bulletinMediator);

		/// <summary>
		/// Closes the dashboard, if one is open.
		/// </summary>
		void CloseDashboard();

		void ShowGraph(RecordList records, string selectedPattern, string sourceFilePath);
		
		bool TryShowGoTo(string defaultValue, out string userValue);

		bool TryGetExpressions(string defaultRegex, string recordsDescription, out string regularExpression);

		bool TryGetThreshold(
			string defaultRegex,
			string recordsDescription,
			string defaultThreshold,
			string defaultComparison,
			out string regularExpression,
			out string threshold,
			out string comparison);
	}
}