namespace BlueDotBrigade.Weevil.Statistics
{
	public sealed class MedianCalculator : ICalculator
	{
		public string Name => "Median";
		public string Description => "Middle value in sorted list";
		public string BestFor => "Ignoring outliers to show central tendency";

		public double? Calculate(IReadOnlyList<double> values)
		{
			if (values.Count == 0) return null;

			var sorted = values.OrderBy(v => v).ToArray();
			var mid = sorted.Length / 2;

			var median = (sorted.Length % 2 == 0)
				? (sorted[mid - 1] + sorted[mid]) / 2.0
				: sorted[mid];

			return Math.Round(median, 3);
		}
	}
}