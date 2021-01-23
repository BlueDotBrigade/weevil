namespace BlueDotBrigade.Weevil.Filter.Expressions
{
	using System;

	public static class ExpressionHelper
	{
		private const string Delimiter = "__";

		public static string GetFriendlyParameterName(string key)
		{
			var result = string.Empty;

			if (!string.IsNullOrWhiteSpace(key))
			{
				result = key.Substring(0, key.IndexOf(Delimiter, StringComparison.InvariantCultureIgnoreCase));
			}

			return result;
		}
	}
}
