namespace BlueDotBrigade.Weevil.IO
{
	public interface IUserDialog
	{
		string ShowUserPrompt(string title, string prompt);
		string ShowUserPrompt(string title, string prompt, string defaultValue);

		bool TryShowFind(string defaultValue, out bool isCaseSensitive, out bool findNext, out string findText);
	}
}
