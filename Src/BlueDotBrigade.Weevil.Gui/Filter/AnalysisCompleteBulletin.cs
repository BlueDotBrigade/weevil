namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;

	internal class AnalysisCompleteBulletin
	{
		private static readonly IDictionary<string, object> NoData = new Dictionary<string, object>();

		public AnalysisCompleteBulletin(int flaggedRecords) : this (flaggedRecords, NoData)
		{
			// nothing to do
		}

		public AnalysisCompleteBulletin(int flaggedRecords, IDictionary<string, object> data)
		{
			this.FlaggedRecords = flaggedRecords;
			this.Data = Convert(data);
		}

		public int FlaggedRecords { get;  }

		public IDictionary<string, string> Data { get; }

		private static IDictionary<string, string> Convert(IDictionary<string, object> data)
		{
			var result = new Dictionary<string, string>();

			foreach (var kvp in data)
			{
				if (kvp.Value is double doubleValue)
				{
					result.Add(kvp.Key, doubleValue.ToString("N3", CultureInfo.InvariantCulture));
				}
				else if (kvp.Value is int intValue)
				{
					result.Add(kvp.Key, intValue.ToString("N0", CultureInfo.InvariantCulture));
				}
				else
				{
					result.Add(kvp.Key, kvp.Value.ToString());
				}
			}

			return result;
		}
	}
}
