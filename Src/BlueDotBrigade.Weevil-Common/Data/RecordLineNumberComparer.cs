namespace BlueDotBrigade.Weevil.Data
{
	using System.Collections.Generic;

	public sealed class RecordLineNumberComparer : Comparer<IRecord>
	{
		public override int Compare(IRecord x, IRecord y)
		{
			return x.LineNumber.CompareTo(y.LineNumber);
		}
	}
}
