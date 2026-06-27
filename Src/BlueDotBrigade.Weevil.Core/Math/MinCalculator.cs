namespace BlueDotBrigade.Weevil.Math
{
	public sealed class MinCalculator : ICalculator
	{
		public string Name => "Min";
		public string Description => "Smallest numeric value matched";
		public string BestFor => "Detecting best-case performance or smallest size";

		public double? Calculate(IReadOnlyList<double> values)
			=> values.Count == 0 ? null : values.Min();
	}
}
