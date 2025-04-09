namespace BlueDotBrigade.Weevil.Statistics
{
    internal sealed class CountCalculator : ICalculator
    {
        public string Description => "Total number of matches";
        public string BestFor => "Measuring frequency or total occurrences";
     
		public KeyValuePair<string, object> Calculate(IReadOnlyList<double> values, IReadOnlyList<DateTimeOffset> timestamps)
            => new("Count", values.Count);
    }
}
