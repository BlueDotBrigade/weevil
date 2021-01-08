namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.IO;

	internal class DefaultCollectionAnalyzer : IRecordAnalyzer
	{
		private static readonly IDictionary<string, object> NoMetadataFound = new Dictionary<string, object>();

		public string Key => nameof(DefaultCollectionAnalyzer);

		public string DisplayName => string.Empty;

		public int Analyze(ImmutableArray<IRecord> records, string outputDirectory, IUserDialog userDialog)
		{
			const int flaggedRecords = 0;
			return flaggedRecords;
		}

		public IDictionary<string, object> Analyze(params object[] userParameters)
		{
			return NoMetadataFound;
		}
	}
}