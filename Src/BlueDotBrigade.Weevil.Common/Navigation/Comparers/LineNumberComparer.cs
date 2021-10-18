namespace BlueDotBrigade.Weevil.Navigation.Comparers
{
	using BlueDotBrigade.Weevil.Data;

	internal sealed class LineNumberComparer : MagnitudeComparer
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
