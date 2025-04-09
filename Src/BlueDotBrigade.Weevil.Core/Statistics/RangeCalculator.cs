namespace BlueDotBrigade.Weevil.Statistics
{
	internal sealed class RangeCalculator : ICalculator
    {
        public string Description => "Timestamp of the first and last matching log entry";
        public string BestFor => "Determining when a pattern or process began and ended";
     
		public KeyValuePair<string, object> Calculate(IReadOnlyList<double> values, IReadOnlyList<DateTimeOffset> timestamps)
        {
            if (timestamps.Count == 0)
                return new("Range", new RangeResult(null, null));

            return new("Range", new RangeResult(timestamps.Min(), timestamps.Max()));
        }
    }
}
