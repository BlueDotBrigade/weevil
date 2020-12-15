namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Generic;

	internal class DefaultCollectionAnalyzer : IRecordCollectionAnalyzer
	{
		private static readonly IDictionary<string, object> NoMetadataFound = new Dictionary<string, object>();

		public IDictionary<string, object> Analyze(params object[] userParameters)
		{
			return NoMetadataFound;
		}
	}
}