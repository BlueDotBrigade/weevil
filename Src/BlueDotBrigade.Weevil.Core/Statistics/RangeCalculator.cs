namespace BlueDotBrigade.Weevil.Statistics
{
	public sealed class RangeCalculator
	{
		public RangeResult Range { get; private set; }

		public RangeCalculator()
		{
			this.Range = new RangeResult(null, null);
		}

		public RangeResult Calculate(IReadOnlyList<DateTime> timestamps)
		{
			if (timestamps.Count > 0)
			{
				this.Range = new RangeResult(timestamps.Min(), timestamps.Max());
			}

			return this.Range;
		}
	}
}
