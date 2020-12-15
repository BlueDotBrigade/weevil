namespace BlueDotBrigade.Collections.Generic
{
	using System.Collections.Generic;
	using System.Linq;

	public static class DictionaryExtensions
	{
		/// <seealso href="https://codereview.stackexchange.com/a/8993/27137"/>
		public static string ToString<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source,
			string keyValueSeparator, string elementSeparator)
		{
			IEnumerable<string> pairs = source.Select(x => string.Format("{0}{1}{2}", x.Key, keyValueSeparator, x.Value));

			return string.Join(elementSeparator, pairs);
		}

		public static void AddRange<TKey, TValue>(
			this IDictionary<TKey, TValue> destination,
			IEnumerable<KeyValuePair<TKey, TValue>> source)
		{
			foreach (KeyValuePair<TKey, TValue> kvp in source)
			{
				destination.Add(kvp.Key, kvp.Value);
			}
		}
	}
}
