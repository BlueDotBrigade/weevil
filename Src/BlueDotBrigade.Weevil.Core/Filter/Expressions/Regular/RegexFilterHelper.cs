namespace BlueDotBrigade.Weevil.Filter.Expressions.Regular
{
	/// <summary>
	/// Provides helper methods for detecting and fixing common issues with regular expression filter patterns.
	/// </summary>
	public static class RegexFilterHelper
	{
		private const string EscapedDoubleQuote = "\"\"";
		private const string DoubleQuote = "\"";

		/// <summary>
		/// Returns <see langword="true"/> when the <paramref name="expression"/> contains escaped double quotes (<c>""</c>).
		/// </summary>
		/// <remarks>
		/// Users who paste filter expressions from sources that use CSV-style quoting may inadvertently
		/// include escaped double quotes (<c>""</c>). Because most log records contain a single double quote (<c>"</c>),
		/// a filter containing <c>""</c> is unlikely to produce any results.
		/// </remarks>
		public static bool HasEscapedDoubleQuotes(string expression)
		{
			return !string.IsNullOrEmpty(expression) && expression.Contains(EscapedDoubleQuote);
		}

		/// <summary>
		/// Replaces all escaped double quotes (<c>""</c>) in the <paramref name="expression"/> with a single double quote (<c>"</c>).
		/// </summary>
		public static string FixEscapedDoubleQuotes(string expression)
		{
			if (string.IsNullOrEmpty(expression))
			{
				return expression;
			}

			return expression.Replace(EscapedDoubleQuote, DoubleQuote);
		}
	}
}
