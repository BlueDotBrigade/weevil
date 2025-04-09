namespace BlueDotBrigade.Weevil.Statistics
{
    internal sealed class MeanCalculator : ICalculator
    {
        public string Description => "Total sum ÷ count";
        public string BestFor => "General overview of value magnitude";
    {
        public KeyValuePair<string, object> Calculate(IReadOnlyList<double> values, IReadOnlyList<DateTimeOffset> timestamps)
            => new("Mean", values.Count == 0 ? null : values.Average());
    }
}