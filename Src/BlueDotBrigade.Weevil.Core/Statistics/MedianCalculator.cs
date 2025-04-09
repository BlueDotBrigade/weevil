namespace BlueDotBrigade.Weevil.Statistics
{
internal sealed class MedianCalculator : ICalculator
    {
        public string Description => "Middle value in sorted list";
        public string BestFor => "Ignoring outliers to show central tendency";
    {
        public KeyValuePair<string, object> Calculate(IReadOnlyList<double> values, IReadOnlyList<DateTimeOffset> timestamps)
        {
            if (values.Count == 0) return new("Median", null);

            var sorted = values.OrderBy(v => v).ToArray();
            int mid = sorted.Length / 2;

            double median = (sorted.Length % 2 == 0)
                ? (sorted[mid - 1] + sorted[mid]) / 2.0
                : sorted[mid];

            return new("Median", median);
        }
    }
}