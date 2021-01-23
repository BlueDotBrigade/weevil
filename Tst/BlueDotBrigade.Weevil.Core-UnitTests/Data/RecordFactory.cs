namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;

	internal static class RecordFactory
	{
		private static readonly char[] RecordDelimiters = new[]
		{
			'\r',
			'\n',
		};

		/// <summary>
		/// In order to avoid creating dependencies on other Weevil classes,
		/// this method should be used to deserialize text into instances of <see cref="IRecord"/>.
		/// </summary>
		/// <param name="serializedData">Contains the records to be deserialized.</param>
		public static ImmutableArray<IRecord> GetRecords(string serializedData)
		{
			var results = new List<IRecord>();

			var lines = serializedData.Split(RecordDelimiters);

			foreach (var line in lines)
			{
				if (!string.IsNullOrEmpty(line))
				{
					results.Add(new Record(
						lineNumber: results.Count + 1,
						createdAt: DateTime.Now,
						severity: SeverityType.Information,
						content: line));
				}
			}

			return ImmutableArray.Create(results.ToArray());
		}
	}
}
