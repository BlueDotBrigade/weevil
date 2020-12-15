namespace BlueDotBrigade.Weevil.Data
{
	using System.Collections.Generic;

	public sealed class RecordIsPinnedComparer : Comparer<IRecord>
	{
		public override int Compare(IRecord x, IRecord y)
		{
			return x.Metadata.IsPinned.CompareTo(y.Metadata.IsPinned);
		}
	}
}
