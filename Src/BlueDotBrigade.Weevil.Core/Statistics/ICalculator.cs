namespace BlueDotBrigade.Weevil.Statistics
{
	internal interface ICalculator
	{
		string Description { get; }
		string BestFor { get; }

		KeyValuePair<string, object> Calculate(IReadOnlyList<double> values, IReadOnlyList<DateTime> timestamps);
	}
}
