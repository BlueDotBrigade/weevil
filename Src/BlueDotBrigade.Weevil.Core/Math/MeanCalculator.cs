namespace BlueDotBrigade.Weevil.Math
{
	using MathNet.Numerics.Statistics;

	public sealed class MeanCalculator : ICalculator
	{
		public string Name => "Mean";
		public string Description => "Total sum ÷ count";
		public string BestFor => "General overview of value magnitude";

		public double? Calculate(IReadOnlyList<double> values)
		{
			if (values.Count == 0) return null;

			return System.Math.Round(values.Mean(), 3);
		}
	}
}
