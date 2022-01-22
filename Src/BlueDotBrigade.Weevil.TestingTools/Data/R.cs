namespace BlueDotBrigade.Weevil.TestingTools.Data
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;

	/// <summary>
	/// A record factory that uses convention over configuration
	/// to create fake records for testing.
	/// </summary>
	public class R
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
			return WithCreatedAt(lineNumber, DateTime.Parse(timestamp));
		}

		public R WithCreatedAt(int lineNumber, DateTime timestamp)
		{
			var record = new Record(
				lineNumber,
				timestamp,
				SeverityType.Information,
				$"Fake record used for testing. Has line number: {_lineNumber}",
				new Metadata());

			_records.Add(record);

			return this;
		}

		public R WithContent(string content)
		{
			_lineNumber++;

			_records.Add(WithContent(_lineNumber, content));

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

		/// <summary>
		/// Creates a fake record with the provided <paramref name="content"/>.
		/// </summary>
		public static IRecord WithContent(int lineNumber, string content)
		{
			return new Record(
				lineNumber,
				DateTime.Now.AddSeconds(lineNumber),
				SeverityType.Information,
				content,
				new Metadata());
		}
	}
}
