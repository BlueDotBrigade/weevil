namespace BlueDotBrigade.Weevil.Data
{
	using System;

	/// <summary>
	/// For each record a view shows, the time since the previous record it shows that has a timestamp, kept by line number (#1016).
	/// </summary>
	/// <remarks>
	/// The view calculates them after each filter, for all its results, then replaces the previous ones.
	/// They never change once calculated, so they can be read from many threads.
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
		public static readonly ElapsedTimes None = new ElapsedTimes(Array.Empty<TimeSpan>());

		private readonly TimeSpan[] _byLineNumber;

		private ElapsedTimes(TimeSpan[] byLineNumber)
		{
			_byLineNumber = byLineNumber;
		}

		/// <summary>
		/// Calculates the elapsed times of the records a view shows, in the order it shows them.
		/// </summary>
		/// <remarks>
		/// Records without a timestamp are skipped: the next record with one is measured from the last record that had one.
		/// The first record with a timestamp has no elapsed time.
		/// </remarks>
		public static ElapsedTimes Calculate(RecordList records)
		{
			if (records is null || records.IsEmpty)
			{
				return None;
			}

			// Index loops, which are faster than foreach over a RecordList: this runs after every filter (#1136 review).
			var count = records.Count;

			var lastLineNumber = 0;
			for (var position = 0; position < count; position++)
			{
				lastLineNumber = System.Math.Max(lastLineNumber, records[position].LineNumber);
			}

			var byLineNumber = new TimeSpan[lastLineNumber + 1];
			Array.Fill(byLineNumber, Unknown);

			IRecord previous = Record.Dummy;
			for (var position = 0; position < count; position++)
			{
				IRecord current = records[position];

				if (current.HasCreationTime)
				{
					if (!Record.IsDummyOrNull(previous) && current.LineNumber >= 0)
					{
						byLineNumber[current.LineNumber] = current.CreatedAt - previous.CreatedAt;
					}

					previous = current;
				}
			}

			return new ElapsedTimes(byLineNumber);
		}

		/// <summary>
		/// Returns the elapsed time of the record on a line, or <see cref="Unknown"/> if it has none.
		/// </summary>
		public TimeSpan Get(int lineNumber)
		{
			return lineNumber >= 0 && lineNumber < _byLineNumber.Length
				? _byLineNumber[lineNumber]
				: Unknown;
		}
	}
}
