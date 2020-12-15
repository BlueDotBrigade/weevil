namespace BlueDotBrigade.Weevil.Diagnostics
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using Data;

	public static class DictionaryExtensions
	{
		private const int All = -1;

		public static void ToDebugStream(this IDictionary<int, IRecord> dictionary, int count = All)
		{
			if (dictionary == null)
			{
				throw new ArgumentNullException(nameof(dictionary));
			}

			var maxCount = count < 0 ? dictionary.Count : count;
			var index = 0;

			foreach (KeyValuePair<int, IRecord> record in dictionary)
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
