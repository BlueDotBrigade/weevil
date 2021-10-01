namespace BlueDotBrigade.Weevil.Data
{
	using System.Collections.Generic;

	internal sealed class RecordLineNumberComparer : Comparer<IRecord>
	{
		public override int Compare(IRecord x, IRecord y)
		{
			return x.LineNumber.CompareTo(y.LineNumber);
		}
	}
}
