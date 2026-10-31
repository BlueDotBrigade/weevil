namespace BlueDotBrigade.Weevil.Navigation.Comparers
{
	using System.Collections.Generic;
	using BlueDotBrigade.Weevil.Data;

	internal abstract class MagnitudeComparer<T> : Comparer<T>
	{
		/// <summary>
		/// Reads the value this comparer compares from the record at <paramref name="position"/>, without creating the record (#1137).
		/// </summary>
		public abstract T Read(RecordList records, int position);

		/// <summary>
		/// Retrieves the difference between two values.
		/// </summary>
		/// <remarks>
		/// Instead of returning the difference between two values, a <see cref="Comparer{T}"/> often returns one of the following: -1, 0 or 1.
		///
		/// <see cref="MagnitudeComparer{T}"/> on the other hand does return the difference between two values.
		/// </remarks>
		public abstract double CompareMagnitude(T x, T y);
	}
}
