namespace BlueDotBrigade.Weevil
{
	using System;

	public static class TimeSpanExtensions
	{
		private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);
		private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);

		private static readonly string NotSpecifiedFormat = @"--.---";

		public static string ToHumanReadable(this TimeSpan value)
		{
			var result = NotSpecifiedFormat;

			if (value < TimeSpan.Zero)
			{
				result = NotSpecifiedFormat;
			}
			else
			{
				if (value < OneMinute)
				{
					result = value.ToString(@"ss\.fff\s");
				}
				else
				{
					if (value > OneDay)
					{
						result = value.ToString(@"d\.hh\:mm\:ss");
					}
					else
					{
						result = value.ToString(@"hh\:mm\:ss");
					}
				}
			}

			return result;
		}
	}
}
