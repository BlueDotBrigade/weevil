namespace BlueDotBrigade.Weevil.Data.Comparers
{
	using System.Collections.Generic;

	internal sealed class IsPinnedComparer : Comparer<IRecord>
	{
		public override int Compare(IRecord x, IRecord y)
		{
			return x.Metadata.IsPinned.CompareTo(y.Metadata.IsPinned);
		}
	}
}
