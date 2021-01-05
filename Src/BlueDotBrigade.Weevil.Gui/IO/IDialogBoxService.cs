namespace BlueDotBrigade.Weevil.Gui.IO
{
	using BlueDotBrigade.Weevil.IO;

	internal interface IDialogBoxService : IUserDialog
	{
		string ShowOpenFile(string compatibleFileExtensions);

		string ShowSaveFile(string targetDirectory, string targetFileName);
	}
}