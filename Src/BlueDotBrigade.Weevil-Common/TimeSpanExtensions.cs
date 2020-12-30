namespace BlueDotBrigade.Weevil
{
	using System;

	public static class TimeSpanExtensions
	{
		private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);
		private static readonly TimeSpan OneDay = TimeSpan.FromDays(1);

		public static string ToHumanReadable(this TimeSpan value)
		{
			var result = string.Empty;

			if (value < TimeSpan.Zero)
			{
				result = string.Empty;
			}
			else
			{
				if (value < OneMinute)
				{
					result = value.ToString(@"ss\.fff");
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
