namespace BlueDotBrigade.Weevil.Statistics
{
	public sealed class StandardDeviationCalculator : ICalculator
	{
		public string Description => "Square root of the average squared deviation from the mean";
		public string BestFor => "Measuring the spread or variability of data";

		public KeyValuePair<string, object> Calculate(IReadOnlyList<double> values, IReadOnlyList<DateTime> timestamps)
		{
			if (values.Count == 0) return new("StdDev", null);

			var mean = values.Average();
			var sumOfSquaredDifferences = values.Sum(v => (v - mean) * (v - mean));
			var stdDev = Math.Sqrt(sumOfSquaredDifferences / values.Count);

			return new("StdDev", Math.Round(stdDev, 3));
		}
	}
}
