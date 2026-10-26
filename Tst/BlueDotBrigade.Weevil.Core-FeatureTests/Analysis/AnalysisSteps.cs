namespace BlueDotBrigade.Weevil.Analysis
{
        using System.Linq;
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
		public void WhenUsingOutOfOrderTimestampsAnalysisWithAThreshold(TimeSpan threshold)
		{
			var thresholdString = ((int)threshold.TotalMilliseconds).ToString();

			// Only a plugin knows what to ask the user.  Furthermore, the unit test has no idea about the implementation details
			// ... E.g. How many parameters are needed? What types of parameters is the plugin expecting?
			// TODO: re-write the `IUserDialog` interface so that the unit test doesn't care about the implementation details
			var parameterProvider = Substitute.For<IUserDialog>();
			parameterProvider
				.ShowUserPrompt(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
				.Returns(thresholdString);

			this.Context.Engine.Analyzer.Analyze(AnalysisType.OutOfOrderTimestamps, parameterProvider);
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

                [When($"detecting both edges using the regular expression: {X.AnyText}")]
                public void WhenDetectingBothEdgesUsingTheRegularExpression(string regularExpression)
                {
                        // Only a plugin knows what to ask the user.  Furthermore, the unit test has no idea about the implementation details
                        // ... E.g. How many parameters are needed? What types of parameters is the plugin expecting?
                        // TODO: re-write the `IUserDialog` interface so that the unit test doesn't care about the implementation details
                        var parameterProvider = Substitute.For<IUserDialog>();
                        parameterProvider
                                .TryGetExpressions(Arg.Any<string>(), Arg.Any<string>(), out Arg.Any<string>())
                                .Returns(x => { x[2] = regularExpression; return true; });

                        this.Context.Engine.Analyzer.Analyze(AnalysisType.MatchingRecordRuns, parameterProvider);
                }

                [When("detecting stable values using the include filter expressions")]
                public void WhenDetectingStableValuesUsingTheIncludeFilterExpressions()
                {
                        this.Context.Engine.Analyzer.Analyze(AnalysisType.StableValueRuns);
                }

                [Then($"the flagged record count will be {X.WholeNumber}")]
                public void ThenTheFlaggedRecordCountWillBe(int expectedCount)
                {
                        var flaggedRecords = this.Context
                                .Engine
				.Filter
				.Results.Count(x => this.Context.Engine.RecordState.IsFlagged(x));

                        flaggedRecords.Should().Be(expectedCount);
                }

                [Then($"the record on line {X.WholeNumber} will have the comment: {X.AnyText}")]
                public void ThenTheRecordOnLineWillHaveTheComment(int lineNumber, string expectedComment)
                {
                        var record = this.Context
                                .Engine
                                .Filter
                                .Results
                                .First(r => r.LineNumber == lineNumber);

                        this.Context.Engine.Annotations.GetComment(record.LineNumber).Should().Be(expectedComment);
                }

                [When("the critical errors insight is refreshed")]
                public void WhenTheCriticalErrorsInsightIsRefreshed()
                {
                        var insight = new CriticalErrorsInsight();
                        insight.Refresh(this.Context.Engine.Filter.Results);
                        this.Context.LastInsight = insight;
                }

                [When("the temporal anomaly insight is refreshed")]
                public void WhenTheTemporalAnomalyInsightIsRefreshed()
                {
                        var insight = new TemporalAnomalyInsight();
                        insight.Refresh(this.Context.Engine.Filter.Results);
                        this.Context.LastInsight = insight;
                }

                [Then("the insight should have related records")]
                public void ThenTheInsightShouldHaveRelatedRecords()
                {
                        var insight = this.Context.LastInsight;
                        insight.Should().NotBeNull();
                        insight.RelatedRecords.Should().NotBeEmpty();
                }

                [Then("the insight should not have related records")]
                public void ThenTheInsightShouldNotHaveRelatedRecords()
                {
                        var insight = this.Context.LastInsight;
                        insight.Should().NotBeNull();
                        insight.RelatedRecords.Should().BeEmpty();
                }

                [Then("the first related record line number should be greater than 0")]
                public void ThenTheFirstRelatedRecordLineNumberShouldBeGreaterThanZero()
                {
                        var insight = this.Context.LastInsight;
                        insight.Should().NotBeNull();
                        insight.RelatedRecords.Should().NotBeEmpty();
                        insight.RelatedRecords[0].LineNumber.Should().BeGreaterThan(0);
                }

                [When("flagging only the insight's records")]
                public void WhenFlaggingOnlyTheInsightsRecords()
                {
                        this.Context.Engine.Analyzer.FlagOnly(this.Context.LastInsight.RelatedRecords.Select(record => record.LineNumber));
                }

                [Then("the results will be the insight's records")]
                public void ThenTheResultsWillBeTheInsightsRecords()
                {
                        this.Context.Engine.Filter.Results
                                .Select(record => record.LineNumber)
                                .Should()
                                .Equal(this.Context.LastInsight.RelatedRecords.Select(record => record.LineNumber));
                }

        }
}
