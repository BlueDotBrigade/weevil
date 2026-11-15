namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Diagnostics;
	using System.Globalization;
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

		public IRecord Find(string timestamp, RecordSearchType searchType = RecordSearchType.NearestNeighbor)
		{
			var firstRecord = _activeRecord.DataSource.GetFirstCreatedAt();

			if (Record.IsDummyOrNull(firstRecord))
			{
				throw new RecordNotFoundException(
					$"Unable to find record - the collection is empty. Value={timestamp}, SearchType={searchType}");
			}

			var referenceRecord = _activeRecord.Record.HasCreationTime
				? _activeRecord.Record
				: firstRecord;

			(DateTime referenceTime, TimeSpan tolerance) searchValue =
				ConvertToDateTime(referenceRecord, timestamp);

			// Text that is not a time would otherwise be searched as a time before every record (#1225).
			if (searchValue.referenceTime == Record.CreationTimeUnknown)
			{
				throw new RecordNotFoundException(
					$"Unable to find record - the value is not a date or time. Value={timestamp}, SearchType={searchType}");
			}

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
					if (TryParseTimestamp(value, out var requestedTime))
					{
						referenceTime = requestedTime;
						tolerance = TimeSpan.FromDays(1);
					}
				}
				else
				{
					if (TryParseTimestamp(value, out var requestedTime))
					{
						var referenceDate = new DateTime(
							activeRecord.CreatedAt.Year,
							activeRecord.CreatedAt.Month,
							activeRecord.CreatedAt.Day);
						referenceTime = referenceDate + requestedTime.TimeOfDay;

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

		/// <summary>
		/// Reads a date or time that the analyst typed, with the Gregorian calendar that records' timestamps use (#1225).
		/// </summary>
		/// <remarks>
		/// The regional settings still decide the order of the day and month. Only their calendar is replaced, such as the Thai Buddhist one,
		/// which would read 2024 as Gregorian 1481.
		/// </remarks>
		private static bool TryParseTimestamp(string value, out DateTime result)
		{
			return DateTime.TryParse(value, GetGregorianCulture(), DateTimeStyles.None, out result)
				|| DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
		}

		private static CultureInfo GetGregorianCulture()
		{
			CultureInfo current = CultureInfo.CurrentCulture;

			if (current.Calendar is GregorianCalendar)
			{
				return current;
			}

			GregorianCalendar gregorian = current.OptionalCalendars.OfType<GregorianCalendar>().FirstOrDefault();

			if (gregorian is null)
			{
				return CultureInfo.InvariantCulture;
			}

			var culture = (CultureInfo)current.Clone();
			culture.DateTimeFormat.Calendar = gregorian;
			return culture;
		}
	}
}
