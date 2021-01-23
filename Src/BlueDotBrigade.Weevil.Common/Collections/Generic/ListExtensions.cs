namespace BlueDotBrigade.Weevil.Collections.Generic
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;

	public static class ListExtensions
	{
		public static ImmutableArray<T> ToImmutableArray<T>(this IList<T> source)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}

			return ImmutableArray.Create(source.ToArray());
		}

		public static void AddRange<TValue>(
			this IList<TValue> destination,
			IEnumerable<TValue> source)
		{
			if (source == null)
			{
				throw new ArgumentNullException(nameof(source));
			}
			if (destination == null)
			{
				throw new ArgumentNullException(nameof(destination));
			}

			foreach (TValue value in source)
			{
				if (value == null)
				{
					throw new NullReferenceException();
				}
				destination.Add(value);
			}
		}
	}
}
