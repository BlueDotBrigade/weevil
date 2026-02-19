namespace BlueDotBrigade.Weevil.Statistics
{
	public sealed class TrimmedMeanCalculator : ICalculator
	{
		public string Name => "TrimmedMean";
		public string Description => "Average after removing top/bottom extremes";
		public string BestFor => "Smoothing average while reducing outlier impact";

		private readonly double _trimPercent;

		public TrimmedMeanCalculator(double trimPercent)
		{
			_trimPercent = trimPercent;
		}

		public double? Calculate(IReadOnlyList<double> values)
		{
			if (values.Count == 0) return null;

			var sorted = values.OrderBy(v => v).ToArray();
			// Use sorted.Length (not values.Count) to ensure calculations reference the array we're operating on
			int trimCount = (int)(sorted.Length * _trimPercent);

			if (trimCount * 2 >= sorted.Length) return null;

			var trimmed = sorted.Skip(trimCount).Take(sorted.Length - 2 * trimCount);
			return trimmed.Average();
		}
	}
}