namespace BlueDotBrigade.Weevil.Statistics
{
public sealed class MedianCalculator : ICalculator
    {
        public string Description => "Middle value in sorted list";
        public string BestFor => "Ignoring outliers to show central tendency";
     
		public KeyValuePair<string, object> Calculate(IReadOnlyList<double> values, IReadOnlyList<DateTime> timestamps)
        {
            if (values.Count == 0) return new("Median", null);

            var sorted = values.OrderBy(v => v).ToArray();
            var mid = sorted.Length / 2;

            var median = (sorted.Length % 2 == 0)
                ? (sorted[mid - 1] + sorted[mid]) / 2.0
                : sorted[mid];

			return new("Median", Math.Round(median, 3));
        }
    }
}