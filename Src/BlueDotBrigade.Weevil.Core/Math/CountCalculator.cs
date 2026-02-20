namespace BlueDotBrigade.Weevil.Math
{
	public sealed class CountCalculator : ICalculator
	{
		public string Name => "Count";
		public string Description => "Total number of matches";
		public string BestFor => "Measuring frequency or total occurrences";

		public double? Calculate(IReadOnlyList<double> values)
			=> values.Count;
	}
}
