namespace BlueDotBrigade.Weevil.Math
{
	/// <seealso cref="IRecordAnalyzer"/>
	/// <seealso cref="IMetricCollector"/>
	public interface ICalculator
	{
		string Name { get; }
		string Description { get; }
		string BestFor { get; }

		double? Calculate(IReadOnlyList<double> values);
	}
}
