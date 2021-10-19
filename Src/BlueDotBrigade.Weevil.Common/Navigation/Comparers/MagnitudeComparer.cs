namespace BlueDotBrigade.Weevil.Navigation.Comparers
{
	using System.Collections.Generic;
	using BlueDotBrigade.Weevil.Data;

	internal abstract class MagnitudeComparer : Comparer<IRecord>
	{
		/// <summary>
		/// Retrieves the difference between two values.
		/// </summary>
		/// <remarks>
		/// Instead of returning the difference between two values, a <see cref="Comparer{T}"/> often returns one of the following: -1, 0 or 1.
		///
		/// <see cref="MagnitudeComparer"/> on the other hand does return the difference between two values.
		/// </remarks>
		public abstract double CompareMagnitude(IRecord x, IRecord y);
	}
}
