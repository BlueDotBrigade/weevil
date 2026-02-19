namespace BlueDotBrigade.Weevil.Statistics
{
	public sealed class MaxCalculator : ICalculator
	{
		public string Name => "Max";
		public string Description => "Largest numeric value matched.";
		public string BestFor => "Identifying worst-case or max usage/outlier.";

		public double? Calculate(IReadOnlyList<double> values)
			=> values.Count == 0 ? null : values.Max();
	}
}