namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Collections.Immutable;
	using System.Diagnostics;
	using BlueDotBrigade.Weevil.Data;

	[DebuggerDisplay("ActiveIndex={_navigator.ActiveIndex}, LineNumber={_navigator.ActiveRecord.LineNumber}")]
	internal class TimestampNavigator : ITimestampNavigator
	{
		private readonly LineNumberNavigator _navigator;

		public TimestampNavigator(ImmutableArray<IRecord> records)
		{
			_navigator = new LineNumberNavigator(records);
		}

		/// <summary>
		/// Represents the result of the the most recent navigation.
		/// </summary>
		/// <returns>
		/// Returns the index value of the record for the latest filter results.
		/// </returns>
		public int ActiveIndex => _navigator.ActiveIndex;

		internal void SetActiveRecord(int lineNumber)
		{
			_navigator.SetActiveRecord(lineNumber);
		}

		internal void UpdateDataSource(ImmutableArray<IRecord> records)
		{
			_navigator.UpdateDataSource(records);
		}

		/// <summary>
		/// Navigates through pinned records in ascending order (e.g. lines: 2, 4, 8, 16).
		/// </summary>
		/// <returns>
		/// Returns a reference to the next pinned <see cref="Record"/>.
		/// </returns>
		public IRecord GoTo(string value)
		{
			IRecord result = Record.Dummy;

			var requestedTime = ConvertToDateTime(_navigator.ActiveRecord, value);

			if (!string.IsNullOrWhiteSpace(value))
			{
				result = _navigator.GoToFirstMatch(record => record.HasCreationTime && record.CreatedAt >= requestedTime);
			}

			return result;
		}

		// If the day of the month is not provided, then the date defaults to today.
		private static DateTime ConvertToDateTime(IRecord activeRecord, string value)
		{
			var result = Record.CreationTimeUnknown;

			if (activeRecord.HasCreationTime)
			{
				if (value.Contains("/") || value.Contains("-"))
				{
					if (DateTime.TryParse(value, out var requestedTime))
					{
						result = requestedTime;
					}
				}
				else
				{
					if (DateTime.TryParse(value, out var requestedTime))
					{
						result = new DateTime(
							activeRecord.CreatedAt.Year,
							activeRecord.CreatedAt.Month,
							activeRecord.CreatedAt.Day,
							requestedTime.Hour,
							requestedTime.Minute,
							requestedTime.Second);
					}
				}
			}

			return result;
		}
	}
}
