namespace BlueDotBrigade.Weevil.Math
{
	using MathNet.Numerics.Statistics;

	public sealed class StandardDeviationCalculator : ICalculator
	{
		public string Name => "StdDev";
		public string Description => "Square root of the average squared deviation from the mean";
		public string BestFor => "Measuring the spread or variability of data";

		public double? Calculate(IReadOnlyList<double> values)
		{
			if (values.Count == 0) return null;

			var stdDev = values.PopulationStandardDeviation();

			return System.Math.Round(stdDev, 3);
		}
	}
}
