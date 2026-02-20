namespace BlueDotBrigade.Weevil.Math
{
	public sealed class StandardDeviationCalculator : ICalculator
	{
		public string Name => "StdDev";
		public string Description => "Square root of the average squared deviation from the mean";
		public string BestFor => "Measuring the spread or variability of data";

		public double? Calculate(IReadOnlyList<double> values)
		{
			if (values.Count == 0) return null;

			var mean = values.Average();
			var sumOfSquaredDifferences = values.Sum(v => (v - mean) * (v - mean));
			var stdDev = System.Math.Sqrt(sumOfSquaredDifferences / values.Count);

			return System.Math.Round(stdDev, 3);
		}
	}
}
