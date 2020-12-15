namespace BlueDotBrigade.Weevil.Gui.IO
{
	internal interface IDialogBoxService
	{
		string ShowOpenFile(string compatibleFileExtensions);

		string ShowSaveFile(string targetDirectory, string targetFileName);

		string ShowUserPrompt(string title, string prompt);
		string ShowUserPrompt(string title, string prompt, string defaultValue);
	}
}