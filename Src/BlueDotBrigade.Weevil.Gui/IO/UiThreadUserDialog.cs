namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System;
	using BlueDotBrigade.Weevil.Gui.Threading;
	using BlueDotBrigade.Weevil.IO;

	/// <summary>
	/// Shows the prompts of an analyzer running on a background thread on the UI thread instead (#996).
	/// </summary>
	/// <remarks>
	/// The analyzer waits for the user's answer, while the UI thread stays free to show the prompt.
	/// </remarks>
	internal class UiThreadUserDialog : IUserDialog
	{
		private readonly IUserDialog _dialog;
		private readonly IUiDispatcher _uiDispatcher;
		private readonly Action _promptOpening;
		private readonly Action _promptClosed;

		/// <param name="dialog">The dialog that shows the prompts.</param>
		/// <param name="uiDispatcher">Runs each prompt on the UI thread.</param>
		/// <param name="promptOpening">Runs on the UI thread before each prompt is shown.</param>
		/// <param name="promptClosed">Runs on the UI thread after each prompt is closed.</param>
		public UiThreadUserDialog(IUserDialog dialog, IUiDispatcher uiDispatcher, Action promptOpening, Action promptClosed)
		{
			_dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
			_uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
			_promptOpening = promptOpening ?? throw new ArgumentNullException(nameof(promptOpening));
			_promptClosed = promptClosed ?? throw new ArgumentNullException(nameof(promptClosed));
		}

		public string ShowUserPrompt(string title, string prompt)
		{
			return Show(() => _dialog.ShowUserPrompt(title, prompt));
		}

		public string ShowUserPrompt(string title, string prompt, string defaultValue)
		{
			return Show(() => _dialog.ShowUserPrompt(title, prompt, defaultValue));
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
			(bool IsAccepted, bool IsCaseSensitive, bool FindNext, bool UseRegex, string FindText, bool SearchElapsedTime, int? MinElapsedMs, int? MaxElapsedMs, bool SearchComments) answer = Show(() =>
			{
				var accepted = _dialog.TryShowFind(
					defaultValue,
					out var caseSensitive,
					out var next,
					out var regex,
					out var text,
					out var elapsedTime,
					out var minimum,
					out var maximum,
					out var comments);

				return (accepted, caseSensitive, next, regex, text, elapsedTime, minimum, maximum, comments);
			});

			isCaseSensitive = answer.IsCaseSensitive;
			findNext = answer.FindNext;
			useRegex = answer.UseRegex;
			findText = answer.FindText;
			searchElapsedTime = answer.SearchElapsedTime;
			minElapsedMs = answer.MinElapsedMs;
			maxElapsedMs = answer.MaxElapsedMs;
			searchComments = answer.SearchComments;
			return answer.IsAccepted;
		}

		public bool TryGetExpressions(string defaultRegex, string recordsDescription, out string regularExpression)
		{
			(bool IsAccepted, string RegularExpression) answer = Show(() =>
			{
				var accepted = _dialog.TryGetExpressions(defaultRegex, recordsDescription, out var expression);
				return (accepted, expression);
			});

			regularExpression = answer.RegularExpression;
			return answer.IsAccepted;
		}

		public bool TryGetThreshold(
			string defaultRegex,
			string recordsDescription,
			string defaultThreshold,
			string defaultComparison,
			out string regularExpression,
			out string threshold,
			out string comparison)
		{
			(bool IsAccepted, string RegularExpression, string Threshold, string Comparison) answer = Show(() =>
			{
				var accepted = _dialog.TryGetThreshold(
					defaultRegex,
					recordsDescription,
					defaultThreshold,
					defaultComparison,
					out var expression,
					out var value,
					out var operation);

				return (accepted, expression, value, operation);
			});

			regularExpression = answer.RegularExpression;
			threshold = answer.Threshold;
			comparison = answer.Comparison;
			return answer.IsAccepted;
		}

		private T Show<T>(Func<T> prompt)
		{
			T answer = default;

			_uiDispatcher.Invoke(() =>
			{
				_promptOpening();

				try
				{
					answer = prompt();
				}
				finally
				{
					_promptClosed();
				}
			});

			return answer;
		}
	}
}
