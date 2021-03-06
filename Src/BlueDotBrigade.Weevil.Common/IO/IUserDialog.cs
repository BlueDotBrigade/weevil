﻿namespace BlueDotBrigade.Weevil.IO
{
	public interface IUserDialog
	{
		string ShowUserPrompt(string title, string prompt);
		string ShowUserPrompt(string title, string prompt, string defaultValue);

		bool TryShowFind(out bool findNext, out string findText);
	}
}
