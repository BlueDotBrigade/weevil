namespace BlueDotBrigade.Weevil.Analysis
{
	using BlueDotBrigade.Weevil.IO;
	using Reqnroll;

	[Binding]
	internal sealed class AnalysisSteps : ReqnrollSteps
	{
		public AnalysisSteps(Token token) : base(token)
		{
			// nothing to do
		}

		[When($"using temporal anomaly analysis with a threshold of {X.TimePeriod}")]
		public void WhenUsingTemporalAnomalyAnalysisWithAThresholdOfSec(TimeSpan threshold)
		{
			var thresholdString = ((int)threshold.TotalMilliseconds).ToString();

			// Only a plugin knows what to ask the user.  Furthermore, the unit test has no idea about the implementation details
			// ... E.g. How many parameters are needed? What types of parameters is the plugin expecting?
			// TODO: re-write the `IUserDialog` interface so that the unit test doesn't care about the implementation details
			var parameterProvider = Substitute.For<IUserDialog>();
			parameterProvider
				.ShowUserPrompt(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
				.Returns(thresholdString);

			this.Context.Engine.Analyzer.Analyze(AnalysisType.TemporalAnomaly, parameterProvider);
		}


		[When($"using elapsed time analysis with a threshold of {X.TimePeriod}")]
		public void WhenUsingElapsedTimeAnalysisWithAThresholdOf(TimeSpan threshold)
		{
			var thresholdString = ((int)threshold.TotalMilliseconds).ToString();

			// Only a plugin knows what to ask the user.  Furthermore, the unit test has no idea about the implementation details
			// ... E.g. How many parameters are needed? What types of parameters is the plugin expecting?
			// TODO: re-write the `IUserDialog` interface so that the unit test doesn't care about the implementation details
			var parameterProvider = Substitute.For<IUserDialog>();
			parameterProvider
				.ShowUserPrompt(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
				.Returns(thresholdString);

			this.Context.Engine.Analyzer.Analyze(AnalysisType.ElapsedTime, parameterProvider);
		}

		[Then($"the flagged record count will be {X.WholeNumber}")]
		public void ThenTheFlaggedRecordCountWillBe(int expectedCount)
		{
			var flaggedRecords = this.Context
				.Engine
				.Filter
				.Results.Count(x => x.Metadata.IsFlagged);

			flaggedRecords.Should().Be(expectedCount);
		}

	}
}