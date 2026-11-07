namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Threading;

	/// <summary>
	/// For each record a view shows, the time since the previous record it shows that has a timestamp (#1016).
	/// </summary>
	/// <remarks>
	/// Elapsed times are mostly displayed, and only sometimes filtered on or searched (decided 2026-11-07), so they are
	/// calculated when they are read, and a filter allocates nothing per line (#1150):
	/// - <see cref="Measure"/> calculates the elapsed time of one row of a list, by position. Displaying rows and searching them use it.
	/// - <see cref="Get"/> looks one up by line number. Its first call calculates those of every result and keeps them.
	///
	/// They never change once created, so they can be read from many threads.
	/// A record the view does not show, or one without a timestamp, has none: <see cref="Unknown"/>.
	/// </remarks>
	public sealed class ElapsedTimes
	{
		/// <summary>
		/// The elapsed time of a record that has none.
		/// </summary>
		public static readonly TimeSpan Unknown = TimeSpan.MinValue;

		/// <summary>
		/// No elapsed times: what a view has before its first filter.
		/// </summary>
		public static readonly ElapsedTimes None = new ElapsedTimes(RecordList.Empty);

		private readonly RecordList _results;
		private readonly object _padlock = new object();

		// Calculated by the first call to Get, so the elapsed times of a view that only displays them are never kept by line number.
		private TimeSpan[] _byLineNumber;

		private ElapsedTimes(RecordList results)
		{
			_results = results;
		}

		/// <summary>
		/// Returns the elapsed times of the records a view shows, in the order it shows them. They are calculated when read.
		/// </summary>
		/// <remarks>
		/// Records without a timestamp are skipped: the next record with one is measured from the last record that had one.
		/// The first record with a timestamp has no elapsed time.
		/// </remarks>
		public static ElapsedTimes Calculate(RecordList records)
		{
			return records is null || records.IsEmpty
				? None
				: new ElapsedTimes(records);
		}

		/// <summary>
		/// Returns the elapsed time of the record at <paramref name="position"/> in <paramref name="records"/>,
		/// or <see cref="Unknown"/> if it has none.
		/// </summary>
		/// <remarks>
		/// It reads only timestamps and line numbers, so it creates no record (#1137) and allocates nothing.
		/// A record without a timestamp returns at once; one with a timestamp reads back over the records before it that have none.
		/// For a view's results, whose line numbers are unique, it returns what <see cref="Get"/> returns for the line at <paramref name="position"/>.
		/// </remarks>
		public static TimeSpan Measure(RecordList records, int position)
		{
			ArgumentNullException.ThrowIfNull(records);

			DateTime createdAt = records.GetCreatedAt(position);

			if (createdAt == Record.CreationTimeUnknown || records.GetLineNumber(position) < 0)
			{
				return Unknown;
			}

			for (var earlier = position - 1; earlier >= 0; earlier--)
			{
				DateTime earlierCreatedAt = records.GetCreatedAt(earlier);

				if (earlierCreatedAt != Record.CreationTimeUnknown)
				{
					// As Get does: a record on the line of Record.Dummy is not measured from.
					return records.GetLineNumber(earlier) == Record.Dummy.LineNumber
						? Unknown
						: createdAt - earlierCreatedAt;
				}
			}

			return Unknown;
		}

		/// <summary>
		/// Returns the elapsed time of the record on a line, or <see cref="Unknown"/> if it has none.
		/// </summary>
		/// <remarks>
		/// The first call reads every result, and keeps their elapsed times by line number: 8 bytes for each line up to the last line shown.
		/// </remarks>
		public TimeSpan Get(int lineNumber)
		{
			TimeSpan[] byLineNumber = Volatile.Read(ref _byLineNumber) ?? CalculateByLineNumber();

			return lineNumber >= 0 && lineNumber < byLineNumber.Length
				? byLineNumber[lineNumber]
				: Unknown;
		}

		private TimeSpan[] CalculateByLineNumber()
		{
			// Under a lock, so threads that read at the same time calculate them once.
			lock (_padlock)
			{
				if (_byLineNumber is null)
				{
					Volatile.Write(ref _byLineNumber, CalculateByLineNumber(_results));
				}

				return _byLineNumber;
			}
		}

		private static TimeSpan[] CalculateByLineNumber(RecordList records)
		{
			if (records.IsEmpty)
			{
				return Array.Empty<TimeSpan>();
			}

			// Reads only line numbers and timestamps, so it creates no record (#1137).
			var count = records.Count;

			var lastLineNumber = 0;
			for (var position = 0; position < count; position++)
			{
				lastLineNumber = System.Math.Max(lastLineNumber, records.GetLineNumber(position));
			}

			var byLineNumber = new TimeSpan[lastLineNumber + 1];
			Array.Fill(byLineNumber, Unknown);

			var previousLineNumber = Record.Dummy.LineNumber;
			var previousCreatedAt = Record.CreationTimeUnknown;
			for (var position = 0; position < count; position++)
			{
				var createdAt = records.GetCreatedAt(position);

				if (createdAt != Record.CreationTimeUnknown)
				{
					var lineNumber = records.GetLineNumber(position);

					if (previousLineNumber != Record.Dummy.LineNumber && lineNumber >= 0)
					{
						byLineNumber[lineNumber] = createdAt - previousCreatedAt;
					}

					previousLineNumber = lineNumber;
					previousCreatedAt = createdAt;
				}
			}

			return byLineNumber;
		}
	}
}
