namespace BlueDotBrigade.Weevil.IO
{
	public interface IUserDialog
	{
		string ShowUserPrompt(string title, string prompt);
		string ShowUserPrompt(string title, string prompt, string defaultValue);

		bool TryShowFind(
			string defaultValue, 
			out bool isCaseSensitive, 
			out bool findNext, 
			out bool useRegex, 
			out string findText,
			out bool searchElapsedTime,
			out int? minElapsedMs,
			out int? maxElapsedMs,
			out bool searchComments);

		bool TryGetExpressions(string defaultRegex, string recordsDescription, out string regularExpression);
	}
}
