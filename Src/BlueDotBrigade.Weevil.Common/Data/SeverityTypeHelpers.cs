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
