namespace BlueDotBrigade.Weevil.Statistics
{
    internal sealed class RangeCalculator : ICalculator
    {
        public string Description => "Timestamp of the first and last matching log entry";
        public string BestFor => "Determining when a pattern or process began and ended";

        public RangeResult Range { get; private set; }

        public RangeCalculator()
        {
            this.Range = new RangeResult(null, null);
        }

        public KeyValuePair<string, object> Calculate(IReadOnlyList<double> values, IReadOnlyList<DateTime> timestamps)
        {
            var timePeriod = TimeSpan.Zero;
            if (timestamps.Count > 0)
            {
                this.Range = new RangeResult(timestamps.Min(), timestamps.Max());
			}

            return new("Range", this.Range);
        }
    }
}
