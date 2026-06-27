namespace BlueDotBrigade.Weevil.Math
{
	using MathNet.Numerics.Statistics;

	public sealed class MedianCalculator : ICalculator
	{
		public string Name => "Median";
		public string Description => "Middle value in sorted list";
		public string BestFor => "Ignoring outliers to show central tendency";

		public double? Calculate(IReadOnlyList<double> values)
		{
			if (values.Count == 0) return null;

			var median = values.Median();

			return System.Math.Round(median, 3);
		}
	}
}
