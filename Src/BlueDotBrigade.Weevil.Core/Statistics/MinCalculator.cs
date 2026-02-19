namespace BlueDotBrigade.Weevil.Statistics
{
    public sealed class MinCalculator : ICalculator
    {
        public string Description => "Smallest numeric value matched";
        public string BestFor => "Detecting best-case performance or smallest size";
     
		public KeyValuePair<string, object> Calculate(IReadOnlyList<double> values, IReadOnlyList<DateTime> timestamps)
            => new("Min", values.Count == 0 ? null : values.Min());
    }
}