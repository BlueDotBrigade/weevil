namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Diagnostics;
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
