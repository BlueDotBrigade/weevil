namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.IO;

	internal interface IDialogBoxService : IUserDialog
	{
		string ShowOpenFile(string compatibleFileExtensions);

		string ShowSaveFile(string targetDirectory, string targetFileName);

		void ShowDashboard(Version weevilVersion, IEngine engine, ImmutableArray<IInsight> insights);

		void ShowGraph(ImmutableArray<IRecord> records, string selectedPattern, IList<string> patternOptions);
		
		bool TryShowGoTo(string defaultValue, out string userValue);
	}
}