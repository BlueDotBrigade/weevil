namespace BlueDotBrigade.Weevil.Statistics
{
    public sealed class MaxCalculator : ICalculator
    {
        public string Description => "Largest numeric value matched.";
        public string BestFor => "Identifying worst-case or max usage/outlier.";

        public KeyValuePair<string, object> Calculate(IReadOnlyList<double> values, IReadOnlyList<DateTime> timestamps)
            => new("Max", values.Count == 0 ? null : values.Max());
    }
}