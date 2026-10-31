namespace BlueDotBrigade.Weevil.Navigation.Comparers
{
	using BlueDotBrigade.Weevil.Data;

	internal sealed class LineNumberComparer : MagnitudeComparer<int>
	{
		public override int Read(RecordList records, int position)
		{
			return records.GetLineNumber(position);
		}

		public override int Compare(int x, int y)
		{
			return x.CompareTo(y);
		}

		public override double CompareMagnitude(int x, int y)
		{
			return x - y;
		}
	}
}
