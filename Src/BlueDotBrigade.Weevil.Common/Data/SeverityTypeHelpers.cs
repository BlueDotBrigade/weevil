namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Generic;

	public static class SeverityTypeHelpers
	{
		private static readonly Dictionary<string, SeverityType> SeverityMap = new Dictionary<string, SeverityType>()
		{
			{"Trace", SeverityType.Verbose},
			{"Verbose", SeverityType.Verbose},
			{"Debug", SeverityType.Debug},
			{"Information", SeverityType.Information},
			{"Info", SeverityType.Information},
			{"Warning", SeverityType.Warning},
			{"Warn", SeverityType.Warning},
			{"Error", SeverityType.Error},
			{"Critical", SeverityType.Critical},
			{"Fatal", SeverityType.Critical},
		};

		private static readonly Dictionary<string, SeverityType>.AlternateLookup<ReadOnlySpan<char>> SeveritySpanLookup =
			SeverityMap.GetAlternateLookup<ReadOnlySpan<char>>();

		// The same names, ignoring case, for text a person types. Logs keep the exact names, so parsing them is unchanged (#1227).
		private static readonly Dictionary<string, SeverityType> SeverityNameMap =
			new Dictionary<string, SeverityType>(SeverityMap, StringComparer.OrdinalIgnoreCase);

		/// <summary>
		/// Converts a severity's name that a person typed, such as in a filter, ignoring case and the white space around it (#1227).
		/// </summary>
		public static bool TryParseIgnoringCase(string value, out SeverityType severityType)
		{
			if (value is not null && SeverityNameMap.TryGetValue(value.Trim(), out severityType))
			{
				return true;
			}

			severityType = SeverityType.Information;
			return false;
		}

		/// <summary>
		/// Converts part of a line to a <see cref="SeverityType"/> without allocating a string.
		/// </summary>
		public static bool TryParse(ReadOnlySpan<char> value, out SeverityType severityType)
		{
			if (SeveritySpanLookup.TryGetValue(value, out severityType))
			{
				return true;
			}

			severityType = SeverityType.Information;
			return false;
		}

		public static bool TryParse(string value, out SeverityType severityType)
		{
			severityType = SeverityType.Information;

			if (SeverityMap.ContainsKey(value))
			{
				severityType = SeverityMap[value];
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}
