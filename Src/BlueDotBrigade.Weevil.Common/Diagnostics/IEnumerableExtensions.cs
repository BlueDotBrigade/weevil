namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using Data;


	public static class IEnumerableExtensions
	{
		private const int All = -1;

		public static void ToDebug(this ICollection<IRecord> source, int maxToDisplay = All)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			var maxCount = maxToDisplay < 0 ? source.Count : maxToDisplay;
			var index = 0;

			foreach (IRecord record in source)
			{
				Debug.WriteLine(record);

				index++;
				if (index > maxCount)
				{
					break;
				}
			}
		}
	}
}
