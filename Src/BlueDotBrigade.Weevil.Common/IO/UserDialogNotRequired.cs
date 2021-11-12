namespace BlueDotBrigade.Weevil.IO
{
	using System;

	/// <summary>
	/// Used when input is not required for an analysis.
	/// </summary>
	public class UserDialogNotRequired : IUserDialog
	{
		public string ShowUserPrompt(string title, string prompt)
		{
			throw new NotImplementedException();
		}

		public string ShowUserPrompt(string title, string prompt, string defaultValue)
		{
			throw new NotImplementedException();
		}

		public bool TryShowFind(string defaultValue, out bool findNext, out string findText)
		{
			throw new NotImplementedException();
		}
	}
}
