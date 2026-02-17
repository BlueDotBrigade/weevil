namespace BlueDotBrigade.Weevil.Statistics
{
 internal sealed class TrimmedMeanCalculator : ICalculator
    {
        public string Description => "Average after removing top/bottom extremes";
        public string BestFor => "Smoothing average while reducing outlier impact";
     
		private readonly double _trimPercent;

        public TrimmedMeanCalculator(double trimPercent)
        {
            _trimPercent = trimPercent;
        }

        public KeyValuePair<string, object> Calculate(IReadOnlyList<double> values, IReadOnlyList<DateTime> timestamps)
        {
            if (values.Count == 0) return new("TrimmedMean", null);

            var sorted = values.OrderBy(v => v).ToArray();
            int trimCount = (int)(values.Count * _trimPercent);

            if (trimCount * 2 >= values.Count) return new("TrimmedMean", null);

            var trimmed = sorted.Skip(trimCount).Take(sorted.Length - 2 * trimCount);
			var trimmedMean = trimmed.Average();

			return new("TrimmedMean", trimmedMean);
        }
    }
}