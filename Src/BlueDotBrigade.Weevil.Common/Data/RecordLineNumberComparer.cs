namespace BlueDotBrigade.Weevil.Data
{
	using System.Collections.Generic;
	internal sealed class RecordLineNumberComparer : MagnitudeComparer
	{
		public override int Compare(IRecord x, IRecord y)
		{
			return x.LineNumber.CompareTo(y.LineNumber);
		}

		public override double CompareMagnitude(IRecord x, IRecord y)
		{
			return x.LineNumber - y.LineNumber;
		}
	}
}
