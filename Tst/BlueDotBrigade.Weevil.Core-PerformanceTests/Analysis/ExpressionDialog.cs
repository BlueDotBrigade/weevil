namespace BlueDotBrigade.Weevil.Analysis
{
	using BlueDotBrigade.Weevil.IO;

	/// <summary>
	/// Answers an analyzer's request for a regular expression, as the user would. The benchmarks use it too (#1177).
	/// </summary>
	internal sealed class ExpressionDialog : IUserDialog
	{
		private readonly string _regularExpression;

		public ExpressionDialog(string regularExpression)
		{
			_regularExpression = regularExpression;
		}

		public bool TryGetExpressions(string defaultRegex, string recordsDescription, out string regularExpression)
		{
			regularExpression = _regularExpression;
			return true;
		}

		public string ShowUserPrompt(string title, string prompt) => throw new NotSupportedException();

		public string ShowUserPrompt(string title, string prompt, string defaultValue) => throw new NotSupportedException();

		public bool TryShowFind(string defaultValue, out bool isCaseSensitive, out bool findNext, out bool useRegex, out string findText, out bool searchElapsedTime, out int? minElapsedMs, out int? maxElapsedMs, out bool searchComments) => throw new NotSupportedException();

		public bool TryGetThreshold(string defaultRegex, string recordsDescription, string defaultThreshold, string defaultComparison, out string regularExpression, out string threshold, out string comparison) => throw new NotSupportedException();
	}
}
