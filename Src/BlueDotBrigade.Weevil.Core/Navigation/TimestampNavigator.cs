namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Diagnostics;
	using System.Linq;
	using BlueDotBrigade.Weevil.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;

	[DebuggerDisplay("ActiveIndex={_navigator.ActiveIndex}, LineNumber={_navigator.ActiveRecord.LineNumber}")]
	internal class TimestampNavigator : ITimestampNavigator
	{
		private readonly RecordNavigator _navigator;

		public TimestampNavigator(RecordNavigator navigator)
		{
			_navigator = navigator;
		}

		public IRecord Find(string value)
		{
			var firstRecord = _navigator.Records.GetFirstCreatedAt();

			if (Record.IsDummyOrNull(firstRecord))
			{
				throw new RecordNotFoundException(-1, "Unable to go timestamp - there must be at least one record with a valid creation time.");
			}

			(DateTime referenceTime, TimeSpan tolerance) searchValue =
				ConvertToDateTime(firstRecord, value);

			switch (SearchType.ClosestMatch)
			{
				case SearchType.ExactMatch:
					// TODO: refactor code... weird we don't get index here
					return _navigator.SetActiveLineNumber(0);

				case SearchType.ClosestMatch:
					var index = _navigator.Records.IndexOfCreatedAt(searchValue.referenceTime, SearchType.ClosestMatch);
					var closestLineNumber = _navigator.Records[index].LineNumber;
					return _navigator.SetActiveLineNumber(closestLineNumber);

				//default:
				//	throw new ArgumentOutOfRangeException(nameof(searchType), searchType, null);
			}
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
