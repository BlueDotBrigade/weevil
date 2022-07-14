namespace BlueDotBrigade.Weevil
{
	using System;

	public static class TimeSpanExtensions
	{
		private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);
		private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);

		private static readonly string NotSpecifiedFormat = @"--.---";

		private const string SecondFormat = @"ss\.fff\s";
		private const string HourAndMinuteFormat = @"hh\:mm\:ss";
		private const string DayFormat = @"d\.hh\:mm\:ss";

		private const string NegativeSecondFormat = @"\-ss\.fff\s";
		private const string NegativeHourFormat = @"\-hh\:mm\:ss";
		private const string NegativeDayFormat = @"\-d\.hh\:mm\:ss";

		public static string ToHumanReadable(this TimeSpan value)
		{
			var result = NotSpecifiedFormat;

			if (value == TimeSpan.MinValue || value == TimeSpan.MaxValue)
			{
				result = NotSpecifiedFormat;
			}
			else
			{
				var isNegative = value < TimeSpan.Zero;

				if (isNegative)
				{
					value = value * -1;
				}

				var format = string.Empty;

				if (value < OneMinute)
				{
					format = isNegative
						? NegativeSecondFormat
						: SecondFormat;
				}
				else
				{
					if (value > OneDay)
					{
						format = isNegative
							? NegativeDayFormat
							: DayFormat;
					}
					else
					{
						format = isNegative
							? NegativeHourFormat
							: HourAndMinuteFormat;
					}
				}

				result = value.ToString(format);
			}

			return result;
		}
	}
}
