namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Generic;

	public interface IRecordCollectionAnalyzer
	{
		IDictionary<string, object> Analyze(params object[] userParameters);
	}
}
