namespace BlueDotBrigade.Weevil.Navigation.Comparers
{
	using System.Collections.Generic;
	using BlueDotBrigade.Weevil.Data;

	internal sealed class IsPinnedComparer : Comparer<IRecord>
	{
		public override int Compare(IRecord x, IRecord y)
		{
			return x.Metadata.IsPinned.CompareTo(y.Metadata.IsPinned);
		}
	}
}
