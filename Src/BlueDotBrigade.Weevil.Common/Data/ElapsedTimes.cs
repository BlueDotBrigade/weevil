namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Immutable;

	/// <summary>
	/// For each record a view shows, the time since the previous record it shows that has a timestamp, kept by line number (#1016).
	/// </summary>
	/// <remarks>
	/// The view calculates them after each filter, for all its results, then replaces the previous ones.
	/// They never change once calculated, so they can be read from many threads.
	/// A record the view does not show, or one without a timestamp, has none: <see cref="Metadata.ElapsedTimeUnknown"/>.
	/// </remarks>
	public sealed class ElapsedTimes
	{
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
		public static ElapsedTimes Calculate(ImmutableArray<IRecord> records)
		{
			if (records.IsDefaultOrEmpty)
			{
				return None;
			}

			var lastLineNumber = 0;
			foreach (IRecord record in records)
			{
				lastLineNumber = System.Math.Max(lastLineNumber, record.LineNumber);
			}

			var byLineNumber = new TimeSpan[lastLineNumber + 1];
			Array.Fill(byLineNumber, Metadata.ElapsedTimeUnknown);

			IRecord previous = Record.Dummy;
			foreach (IRecord current in records)
			{
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
		/// Returns the elapsed time of the record on a line, or <see cref="Metadata.ElapsedTimeUnknown"/> if it has none.
		/// </summary>
		public TimeSpan Get(int lineNumber)
		{
			return lineNumber >= 0 && lineNumber < _byLineNumber.Length
				? _byLineNumber[lineNumber]
				: Metadata.ElapsedTimeUnknown;
		}
	}
}
