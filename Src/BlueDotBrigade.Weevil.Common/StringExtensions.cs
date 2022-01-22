namespace BlueDotBrigade.Weevil
{
	using System;

	public static class StringExtensions
	{
		/// <summary>
		/// Returns a value indicating whether a specified string occurs within this string, using the specified comparison rules.
		/// </summary>
		/// <param name="text">Represents that text that is being searched.</param>
		/// <param name="value">Represents the value to search for.</param>
		/// <param name="comparision">One of the enumeration values that specifies the rules to use in the comparison.</param>
		/// <returns><see langword="true"/> if the value parameter occurs within this string; otherwise, <see langword="false"/>.</returns>
		/// <remarks>
		/// To perform a case insensitive search use <see cref="StringComparison.OrdinalIgnoreCase"/>.
		/// </remarks>
		public static bool Contains(this string text, string value, StringComparison comparision)
		{
			return text.IndexOf(value, comparision) >= 0;
		}
	}
}
