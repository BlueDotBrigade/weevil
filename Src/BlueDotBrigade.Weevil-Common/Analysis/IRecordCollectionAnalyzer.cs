namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.IO;

	public interface IRecordCollectionAnalyzer
	{
		string Key { get; }

		string DisplayName { get; }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="user">Enables the analyzer to prompt the user for additional context.</param>
		/// <param name="outputDirectory">Path to where analysis results can be saved.</param>
		/// <param name="records">List of records to be analyzed.</param>
		/// <returns>Number of flagged records.</returns>
		int Analyze(ImmutableArray<IRecord> records, string outputDirectory, IUserDialog user);
	}
}
