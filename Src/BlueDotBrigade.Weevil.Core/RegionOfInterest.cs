namespace BlueDotBrigade.Weevil
{
	internal class RegionOfInterest
	{
		public int StartIndex { get; }
		public int EndIndex { get; }

		public RegionOfInterest(int startIndex, int endIndex)
		{
			this.StartIndex = startIndex;
			this.EndIndex = endIndex;
		}

		public bool OverlapsWith(RegionOfInterest other)
		{
			return this.StartIndex <= other.EndIndex && this.EndIndex >= other.StartIndex;
		}

		public bool Contains(int recordIndex)
		{
			return recordIndex >= this.StartIndex && recordIndex <= this.EndIndex;
		}
	}
}