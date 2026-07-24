namespace BlueDotBrigade.Weevil.Analysis
{
        using System;
        using System.IO;
        using System.Linq;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.IO;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using NSubstitute;

	[TestClass]
	public class AnalysisShould
	{
		[TestMethod]
		public void FlagRecordsWhenDataTransitionDetected()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine.Filter.Apply(
				FilterType.RegularExpression,
				new FilterCriteria(@"to new state (?<State>.*)"));

			engine
				.Analyzer
				.Analyze(AnalysisType.StateTransitions);

			foreach (IRecord record in engine.Filter.Results)
			{
				switch (record.LineNumber)
				{
					case 100:
					case 200:
					case 300:
					case 400:
					case 500:
						record.Metadata.IsFlagged.Should().BeTrue();
						break;

					default:
						record.Metadata.IsFlagged.Should().BeFalse();
						break;
				}
			}
		}

                [TestMethod]
                public void AddCommentWhenDataTransitionDetected()
                {
                        IEngine engine = Engine
                                .UsingPath(new Daten().AsFilePath(From.GlobalDefault))
                                .Open();

                        engine.Filter.Apply(
                                FilterType.RegularExpression,
                                new FilterCriteria(@"to new state (?<State>.*)"));

                        engine
                                .Analyzer
                                .Analyze(AnalysisType.StateTransitions);

                        foreach (IRecord record in engine.Filter.Results)
                        {
                                switch (record.LineNumber)
                                {
                                        case 100:
                                        case 200:
                                        case 300:
                                        case 400:
                                        case 500:
                                                record.Metadata.HasComment.Should().BeTrue();
                                                break;
                                }
                        }
                }

                [TestMethod]
                public void DetectStableValuesFlagsStartAndStop()
                {
                        var filePath = CreateStableValueLog();

                        try
                        {
                                IEngine engine = Engine
                                        .UsingPath(filePath)
                                        .Open();

                                engine.Filter.Apply(
                                        FilterType.RegularExpression,
                                        new FilterCriteria(@"Temperature=(?<State>\w+)"));

                                engine.Analyzer.Analyze(AnalysisType.StableValueRuns);

                                int[] flaggedLines = engine
                                        .Filter
                                        .Results
                                        .Where(record => record.Metadata.IsFlagged)
                                        .Select(record => record.LineNumber)
                                        .ToArray();

                                CollectionAssert.AreEquivalent(
                                        new[] { 1, 3, 4, 5, 6 },
                                        flaggedLines);

                                (engine.Filter.Results.Count(r => r.Metadata.IsFlagged)).Should().Be(5);
                        }
                        finally
                        {
                                TryDelete(filePath);
                        }
                }

                [TestMethod]
                public void DetectStableValuesAnnotatesComments()
                {
                        var filePath = CreateStableValueLog();

                        try
                        {
                                IEngine engine = Engine
                                        .UsingPath(filePath)
                                        .Open();

                                engine.Filter.Apply(
                                        FilterType.RegularExpression,
                                        new FilterCriteria(@"Temperature=(?<State>\w+)"));

                                engine
                                        .Analyzer
                                        .Analyze(AnalysisType.StableValueRuns);

                                var recordsByLineNumber = engine
                                        .Filter
                                        .Results
                                        .ToDictionary(record => record.LineNumber);

                                recordsByLineNumber[1].Metadata.Comment.Should().Be("Start State: Cold");
                                recordsByLineNumber[3].Metadata.Comment.Should().Be("Stop State: Cold");
                                recordsByLineNumber[4].Metadata.Comment.Should().Be("Start State: Warm");
                                recordsByLineNumber[5].Metadata.Comment.Should().Be("Stop State: Warm");
                                recordsByLineNumber[6].Metadata.Comment.Should().Be("Start State: Hot, Stop State: Hot");
                        }
                        finally
                        {
                                TryDelete(filePath);
                        }
                }

                // HACK: This integration test should be a unit test. It isn't because the analyzer depends on `FilterStrategy` (a complex object) as an input. Code smell.
                [TestMethod]
                public void DetectRisingEdges()
                {
			var detectRisingValue = @"Value=(?<Value>\d+)";
			var filePath = CreateRisingEdgeLog();

			try
			{
				var engine = Engine
					.UsingPath(filePath)
					.Open();

				engine.Filter.Apply(FilterType.RegularExpression, new FilterCriteria(detectRisingValue));

				engine.Analyzer.Analyze(AnalysisType.DetectRisingEdges, CreateAscendingAnalysisDialog(detectRisingValue));

				var flaggedRecords = engine
					.Filter.Results
					.Count(x => x.Metadata.IsFlagged);

				flaggedRecords.Should().Be(1);
			}
			finally
			{
				TryDelete(filePath);
			}
		}

		[TestMethod]
                public void DetectFallingEdges()
                {
                        var detectFallingValue = @"Value=(?<Value>\d+)";
			var filePath = CreateFallingEdgeLog();

			try
			{
				var engine = Engine
					.UsingPath(filePath)
					.Open();

				engine.Filter.Apply(FilterType.RegularExpression, new FilterCriteria(detectFallingValue));

				engine.Analyzer.Analyze(AnalysisType.DetectFallingEdges, CreateAscendingAnalysisDialog(detectFallingValue));

				var flaggedRecords = engine
					.Filter.Results
					.Count(x => x.Metadata.IsFlagged);

				flaggedRecords.Should().Be(1);
			}
			finally
			{
				TryDelete(filePath);
			}
                }

		[TestMethod]
		public void GivenOverlappingOrExpressions_WhenDetectingStateTransitions_ThenDoesNotCreateTransitionsWithinOneRecord()
		{
			// Regression: Issue #926
			var filePath = CreateOverlappingOrLog();

			try
			{
				var engine = Engine
					.UsingPath(filePath)
					.Open();

				engine.Filter.Apply(
					FilterType.RegularExpression,
					new FilterCriteria(@"A=(?<Value>\d+)||B=(?<Value>\d+)"));

				engine.Analyzer.Analyze(AnalysisType.StateTransitions);

				engine.Filter.Results.Count(record => record.Metadata.IsFlagged).Should().Be(0);
			}
			finally
			{
				TryDelete(filePath);
			}
		}

		[TestMethod]
		public void GivenOverlappingOrExpressions_WhenDetectingStableValueRuns_ThenDoesNotStartOrStopRunsWithinOneRecord()
		{
			// Regression: Issue #926
			var filePath = CreateOverlappingOrLog();

			try
			{
				var engine = Engine
					.UsingPath(filePath)
					.Open();

				engine.Filter.Apply(
					FilterType.RegularExpression,
					new FilterCriteria(@"A=(?<Value>\d+)||B=(?<Value>\d+)"));

				engine.Analyzer.Analyze(AnalysisType.StableValueRuns);

				engine.Filter.Results.Count(record => record.Metadata.IsFlagged).Should().Be(0);
			}
			finally
			{
				TryDelete(filePath);
			}
		}

		[TestMethod]
		public void GivenOverlappingOrExpressions_WhenDetectingRisingEdges_ThenDoesNotCreateEdgesWithinOneRecord()
		{
			// Regression: Issue #926
			var filePath = CreateOverlappingOrLog();

			try
			{
				var engine = Engine
					.UsingPath(filePath)
					.Open();

				engine.Filter.Apply(
					FilterType.RegularExpression,
					new FilterCriteria(@"A=(?<Value>\d+)||B=(?<Value>\d+)"));

				engine.Analyzer.Analyze(AnalysisType.DetectRisingEdges, CreateAscendingAnalysisDialog(@"A=(?<Value>\d+)||B=(?<Value>\d+)"));

				engine.Filter.Results.Count(record => record.Metadata.IsFlagged).Should().Be(0);
			}
			finally
			{
				TryDelete(filePath);
			}
		}

		[TestMethod]
		public void GivenOverlappingOrExpressions_WhenDetectingFallingEdges_ThenDoesNotCreateEdgesWithinOneRecord()
		{
			// Regression: Issue #926
			var filePath = CreateOverlappingOrLog();

			try
			{
				var engine = Engine
					.UsingPath(filePath)
					.Open();

				engine.Filter.Apply(
					FilterType.RegularExpression,
					new FilterCriteria(@"A=(?<Value>\d+)||B=(?<Value>\d+)"));

				engine.Analyzer.Analyze(AnalysisType.DetectFallingEdges, CreateAscendingAnalysisDialog(@"A=(?<Value>\d+)||B=(?<Value>\d+)"));

				engine.Filter.Results.Count(record => record.Metadata.IsFlagged).Should().Be(0);
			}
			finally
			{
				TryDelete(filePath);
			}
		}

                private static string CreateStableValueLog()
                {
                        var lines = new[]
                        {
                                "Info 1900-01-01 12:00:00.0000 248 Temperature=Cold",
                                "Info 1900-01-01 12:00:01.0000 248 Temperature=Cold",
                                "Info 1900-01-01 12:00:02.0000 248 Temperature=Cold",
                                "Info 1900-01-01 12:00:03.0000 248 Temperature=Warm",
                                "Info 1900-01-01 12:00:04.0000 248 Temperature=Warm",
                                "Info 1900-01-01 12:00:05.0000 248 Temperature=Hot",
                        };

                        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.log");
                        System.IO.File.WriteAllText(filePath, string.Join(Environment.NewLine, lines));

                        return filePath;
                }

		private static string CreateOverlappingOrLog()
		{
			var lines = new[]
			{
				"Info 1900-01-01 12:00:00.0000 248 A=1 B=2",
				"Info 1900-01-01 12:00:01.0000 248 A=1 B=2",
			};

			var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.log");
			System.IO.File.WriteAllText(filePath, string.Join(Environment.NewLine, lines));

			return filePath;
		}

		private static string CreateRisingEdgeLog()
		{
			var lines = new[]
			{
				"1900-01-01 12:00:00.0000\t248\t1\tInformation\tValue=5",
				"1900-01-01 12:00:01.0000\t248\t1\tInformation\tValue=4",
				"1900-01-01 12:00:02.0000\t248\t1\tInformation\tValue=3",
				"1900-01-01 12:00:03.0000\t248\t1\tInformation\tValue=4",
				"1900-01-01 12:00:04.0000\t248\t1\tInformation\tValue=5",
			};

			var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.log");
			System.IO.File.WriteAllText(filePath, string.Join(Environment.NewLine, lines));

			return filePath;
		}

		private static string CreateFallingEdgeLog()
		{
			var lines = new[]
			{
				"1900-01-01 12:00:00.0000\t248\t1\tInformation\tValue=1",
				"1900-01-01 12:00:01.0000\t248\t1\tInformation\tValue=2",
				"1900-01-01 12:00:02.0000\t248\t1\tInformation\tValue=3",
				"1900-01-01 12:00:03.0000\t248\t1\tInformation\tValue=2",
				"1900-01-01 12:00:04.0000\t248\t1\tInformation\tValue=1",
			};

			var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.log");
			System.IO.File.WriteAllText(filePath, string.Join(Environment.NewLine, lines));

			return filePath;
		}

		private static IUserDialog CreateAscendingAnalysisDialog(string expression)
		{
			var userDialog = Substitute.For<IUserDialog>();
			userDialog
				.TryGetExpressions(Arg.Any<string>(), Arg.Any<string>(), out Arg.Any<string>())
				.Returns(x => { x[2] = expression; return true; });
			userDialog
				.ShowUserPrompt(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
				.Returns("Ascending");

			return userDialog;
		}

                private static void TryDelete(string filePath)
                {
                        if (string.IsNullOrWhiteSpace(filePath))
                        {
                                return;
                        }

                        try
                        {
                                if (System.IO.File.Exists(filePath))
                                {
									System.IO.File.Delete(filePath);
                                }
                        }
                        catch (IOException)
                        {
                                // Ignored - best effort cleanup.
                        }
                        catch (UnauthorizedAccessException)
                        {
                                // Ignored - best effort cleanup.
                        }
                }
        }
}