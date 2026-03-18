namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.IO;

	internal interface IDialogBoxService : IUserDialog
	{
		string ShowOpenFile(string compatibleFileExtensions);

		string ShowSaveFile(string targetDirectory, string targetFileName);

		string ShowUserPrompt(string title, string userPrompt);

		bool TryShowUserPrompt(string title, string userPrompt, string validationPattern, string validationError, out string userValue);

		void ShowDashboard(Version weevilVersion, IEngine engine, ImmutableArray<IInsight> insights, IBulletinMediator bulletinMediator);

		void ShowGraph(ImmutableArray<IRecord> records, string selectedPattern, string sourceFilePath);
		
		bool TryShowGoTo(string defaultValue, out string userValue);

		bool TryGetExpressions(string defaultRegex, string recordsDescription, out string regularExpression);
	}
}