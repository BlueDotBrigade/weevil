namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.IO;

	internal interface IDialogBoxService : IUserDialog
	{
		string ShowOpenFile(string compatibleFileExtensions);

		string ShowSaveFile(string targetDirectory, string targetFileName);

		void ShowDashboard(ImmutableArray<IInsight> insights, IEngine engine);

		string ShowGoTo(string defaultValue);
	}
}