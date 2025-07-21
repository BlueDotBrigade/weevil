namespace BlueDotBrigade.Weevil.Statistics
{
	/// <seealso cref="IRecordAnalyzer">
	/// <seealso cref="IMetricCollector">
	internal interface ICalculator
	{
		string Description { get; }
		string BestFor { get; }

		KeyValuePair<string, object> Calculate(IReadOnlyList<double> values, IReadOnlyList<DateTime> timestamps);
	}
}
