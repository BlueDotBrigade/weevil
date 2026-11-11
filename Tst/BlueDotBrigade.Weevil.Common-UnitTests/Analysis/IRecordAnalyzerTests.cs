namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Threading;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.IO;
	using NSubstitute;

	[TestClass]
	public class IRecordAnalyzerTests
	{
		[TestMethod]
		public void GivenAPluginsAnalyzerWithoutCancellation_WhenRunAfterAbort_ThenItDoesNotRun()
		{
			// A plugin built before #1027 implements only the overload without a token, and still works.
			IRecordAnalyzer analyzer = new AnalyzerWithoutCancellation();

			Action analyze = () => analyzer.Analyze(RecordList.Empty, new RecordState(), string.Empty, Substitute.For<IUserDialog>(), true, new CancellationToken(canceled: true));

			analyze.Should().Throw<OperationCanceledException>();
			((AnalyzerWithoutCancellation)analyzer).Runs.Should().Be(0);
		}

		[TestMethod]
		public void GivenAPluginsAnalyzerWithoutCancellation_WhenRunWithoutAbort_ThenItRuns()
		{
			IRecordAnalyzer analyzer = new AnalyzerWithoutCancellation();

			Results results = analyzer.Analyze(RecordList.Empty, new RecordState(), string.Empty, Substitute.For<IUserDialog>(), true, CancellationToken.None);

			results.FlaggedRecords.Should().Be(1);
			((AnalyzerWithoutCancellation)analyzer).Runs.Should().Be(1);
		}

		private sealed class AnalyzerWithoutCancellation : IRecordAnalyzer
		{
			public int Runs { get; private set; }

			public string Key => nameof(AnalyzerWithoutCancellation);

			public string DisplayName => nameof(AnalyzerWithoutCancellation);

			public Results Analyze(RecordList records, IRecordState state, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata)
			{
				this.Runs++;

				return new Results(1);
			}
		}
	}
}
