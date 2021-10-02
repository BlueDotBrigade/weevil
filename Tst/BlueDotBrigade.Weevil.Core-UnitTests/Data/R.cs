namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;

	/// <summary>
	/// A record factory that uses convention over configuration
	/// to create fake records for testing.
	/// </summary>
	internal class R
	{
		private int _lineNumber = 0;

		private readonly IList<IRecord> _records;

		private R()
		{
			_records = new List<IRecord>();
		}

		public static R Create()
		{
			return new R();
		}

		public ImmutableArray<IRecord> GetRecords()
		{
			return _records.ToImmutableArray();
		}

		/// <summary>
		/// Creates a fake record with the given <paramref name="timestamp"/>.
		/// </summary>
		/// <remarks>
		/// Default values:
		/// <list type="bullet">
		/// <item> Line number starts at 1, and increments by 1.</item>
		/// </list>
		/// </remarks>
		public R WithCreatedAt(string timestamp)
		{
			_lineNumber++;
			return WithCreatedAt(_lineNumber, timestamp);
		}
		public R WithCreatedAt(int lineNumber, string timestamp)
		{
			var record = new Record(
				lineNumber,
				DateTime.Parse(timestamp),
				SeverityType.Information,
				$"Fake record used for testing. Has line number: {_lineNumber}",
				new Metadata());

			_records.Add(record);

			return this;
		}

		/// <summary>
		/// Creates a fake record with the provided <paramref name="lineNumber"/>.
		/// </summary>
		public static IRecord WithLineNumber(int lineNumber)
		{
			return new Record(
				lineNumber,
				DateTime.Now.AddSeconds(lineNumber),
				SeverityType.Information,
				$"Fake record used for testing. Has line number: {lineNumber}",
				new Metadata());
		}
	}
}
