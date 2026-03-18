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
			return string.Empty;
		}

		public string ShowUserPrompt(string title, string prompt, string defaultValue)
		{
			return defaultValue ?? string.Empty;
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
			isCaseSensitive = false;
			findNext = true;
			useRegex = false;
			findText = defaultValue ?? string.Empty;
			searchElapsedTime = false;
			minElapsedMs = null;
			maxElapsedMs = null;
			searchComments = false;
			return !string.IsNullOrWhiteSpace(findText);
		}

		public bool TryGetExpressions(string defaultRegex, string recordsDescription, out string regularExpression)
		{
			regularExpression = defaultRegex ?? string.Empty;
			return !string.IsNullOrWhiteSpace(regularExpression);
		}
	}
}
