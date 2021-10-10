namespace BlueDotBrigade.Weevil.Data.Comparers
{
	using System.Collections.Generic;

	internal abstract class MagnitudeComparer : Comparer<IRecord>
	{
		public abstract double CompareMagnitude(IRecord x, IRecord y);
	}
}
