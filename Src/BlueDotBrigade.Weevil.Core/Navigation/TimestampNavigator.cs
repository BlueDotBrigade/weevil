namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Diagnostics;
	using System.Linq;
	using BlueDotBrigade.Weevil.Data;

	[DebuggerDisplay("ActiveIndex={_activeRecord.Index}, LineNumber={_activeRecord.Record.LineNumber}")]
	internal class TimestampNavigator : ITimestampNavigator
	{
		private readonly ActiveRecord _activeRecord;

		public TimestampNavigator(ActiveRecord activeRecord)
		{
			_activeRecord = activeRecord;
		}

		public IRecord Find(string value, RecordSearchType searchType = RecordSearchType.ClosestMatch)
		{
			var firstRecord = _activeRecord.DataSource.GetFirstCreatedAt();

			if (Record.IsDummyOrNull(firstRecord))
			{
				throw new RecordNotFoundException(
					$"Unable to find record - the collection is empty. Value={value}, SearchType={searchType}");
			}

			(DateTime referenceTime, TimeSpan tolerance) searchValue =
				ConvertToDateTime(firstRecord, value);

			var index = _activeRecord.DataSource.IndexOfCreatedAt(searchValue.referenceTime, searchType);
			return _activeRecord.SetActiveIndex(index);
		}


		/// <summary>
		/// Convert the string <paramref name="value"/> to a valid <see cref="DateTime"/> value.
		/// </summary>
		/// <remarks>
		/// If the <paramref name="value"/> does not include the day of the month, then assume the selected record's date should be used.
		/// </remarks>
		private static (DateTime referenceTime, TimeSpan tolerance) ConvertToDateTime(IRecord activeRecord, string value)
		{
			var referenceTime = Record.CreationTimeUnknown;
			var tolerance = TimeSpan.Zero;

			if (activeRecord.HasCreationTime)
			{
				// Search value includes: day of month?
				// ... yes: use given value
				// ... no: use day of month from selected record
				if (value.Contains("/") || value.Contains("-"))
				{
					if (DateTime.TryParse(value, out var requestedTime))
					{
						referenceTime = requestedTime;
						tolerance = TimeSpan.FromDays(1);
					}
				}
				else
				{
					if (DateTime.TryParse(value, out var requestedTime))
					{
						referenceTime = new DateTime(
							activeRecord.CreatedAt.Year,
							activeRecord.CreatedAt.Month,
							activeRecord.CreatedAt.Day,
							requestedTime.Hour,
							requestedTime.Minute,
							requestedTime.Second);

						if (value.Count(c => c == ':') == 1)
						{
							tolerance = TimeSpan.FromMinutes(15); // 11:12
						}
						else
						{
							if (requestedTime.Millisecond == 0)
							{
								tolerance = TimeSpan.FromSeconds(15); // 11:12:13
							}
							else
							{
								tolerance = TimeSpan.FromSeconds(1); // 11:12:13.1234
							}
						}
					}
				}
			}

			return (referenceTime, tolerance);
		}
	}
}
