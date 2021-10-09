namespace BlueDotBrigade.Weevil.Data
{
	using System.Collections.Generic;

	public abstract class MagnitudeComparer : Comparer<IRecord>
	{
		public abstract double CompareMagnitude(IRecord x, IRecord y);
	}
}
