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

		public bool TryShowFind(
			string defaultValue, 
			out bool isCaseSensitive, 
			out bool findNext, 
			out bool useRegex, 
			out string findText,
			out bool searchElapsedTime,
			out int? minElapsedMs,
			out int? maxElapsedMs,
			out bool searchComments)
		{
			throw new NotImplementedException();
		}

		public bool TryShowAnalysisDialog(string defaultRegex, string recordsDescription, out string regularExpression)
		{
			throw new NotImplementedException();
		}
	}
}
