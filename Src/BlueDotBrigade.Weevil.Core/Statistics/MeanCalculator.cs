namespace BlueDotBrigade.Weevil.Statistics
{
	public sealed class MeanCalculator : ICalculator
	{
		public string Description => "Total sum ÷ count";
		public string BestFor => "General overview of value magnitude";

		public KeyValuePair<string, object> Calculate(IReadOnlyList<double> values, IReadOnlyList<DateTime> timestamps)
		{
			double? mean = null;
			if (values.Count > 0)
			{
				mean = values.Average();
				mean = Math.Round(mean.Value, 3);
			}
			return new("Mean", mean);
		}
	}
}