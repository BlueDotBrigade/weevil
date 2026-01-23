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
				.Analyze(AnalysisType.DetectDataTransition);

			foreach (IRecord record in engine.Filter.Results)
			{
				switch (record.LineNumber)
				{
					case 100:
					case 200:
					case 300:
					case 400:
					case 500:
						Assert.IsTrue(record.Metadata.IsFlagged);
						break;

					default:
						Assert.IsFalse(record.Metadata.IsFlagged);
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
                                .Analyze(AnalysisType.DetectDataTransition);

                        foreach (IRecord record in engine.Filter.Results)
                        {
                                switch (record.LineNumber)
                                {
                                        case 100:
                                        case 200:
                                        case 300:
                                        case 400:
                                        case 500:
                                                Assert.IsTrue(record.Metadata.HasComment);
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

                                engine.Analyzer.Analyze(AnalysisType.DetectStableValues);

                                int[] flaggedLines = engine
                                        .Filter
                                        .Results
                                        .Where(record => record.Metadata.IsFlagged)
                                        .Select(record => record.LineNumber)
                                        .ToArray();

                                CollectionAssert.AreEquivalent(
                                        new[] { 1, 3, 4, 5, 6 },
                                        flaggedLines);

                                Assert.AreEqual(5, engine.Filter.Results.Count(r => r.Metadata.IsFlagged));
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
                                        .Analyze(AnalysisType.DetectStableValues);

                                var recordsByLineNumber = engine
                                        .Filter
                                        .Results
                                        .ToDictionary(record => record.LineNumber);

                                Assert.AreEqual("Start State: Cold", recordsByLineNumber[1].Metadata.Comment);
                                Assert.AreEqual("Stop State: Cold", recordsByLineNumber[3].Metadata.Comment);
                                Assert.AreEqual("Start State: Warm", recordsByLineNumber[4].Metadata.Comment);
                                Assert.AreEqual("Stop State: Warm", recordsByLineNumber[5].Metadata.Comment);
                                Assert.AreEqual("Start State: Hot, Stop State: Hot", recordsByLineNumber[6].Metadata.Comment);
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
			var dectectMinuteIncreasing = @"\s12:(?<Minute>[0-9]{2})";

			var engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine.Filter.Apply(FilterType.RegularExpression, new FilterCriteria(dectectMinuteIncreasing));

			// Only a plugin knows what to ask the user.  Furthermore, the unit test has no idea about the implementation details
			// ... E.g. How many parameters are needed? What types of parameters is the plugin expecting?
			// TODO: re-write the `IUserDialog` interface so that the unit test doesn't care about the implementation details
			var userDialog = Substitute.For<IUserDialog>();
			userDialog
				.TryShowAnalysisDialog(Arg.Any<string>(), Arg.Any<string>(), out Arg.Any<string>())
				.Returns(x => { x[2] = dectectMinuteIncreasing; return true; });
			userDialog
				.ShowUserPrompt(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
				.Returns("Ascending");
			engine.Analyzer.Analyze(AnalysisType.DetectRisingEdges, userDialog);

			var flaggedRecords = engine
				.Filter.Results
				.Count(x => x.Metadata.IsFlagged);

			// 8 transitions + 1 for the first value found
			Assert.AreEqual(9, flaggedRecords);
		}

		[TestMethod]
                public void DetectFallingEdges()
                {
                        var detectSecondRollover = @"\s12:[0-9]{2}:(?<Second>[0-9]{2})";

                        var engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			engine.Filter.Apply(FilterType.RegularExpression, new FilterCriteria(detectSecondRollover));

			// Only a plugin knows what to ask the user.  Furthermore, the unit test has no idea about the implementation details
			// ... E.g. How many parameters are needed? What types of parameters is the plugin expecting?
			// TODO: re-write the `IUserDialog` interface so that the unit test doesn't care about the implementation details
			var userDialog = Substitute.For<IUserDialog>();
			userDialog
				.TryShowAnalysisDialog(Arg.Any<string>(), Arg.Any<string>(), out Arg.Any<string>())
				.Returns(x => { x[2] = detectSecondRollover; return true; });
			userDialog
				.ShowUserPrompt(Arg.Any<string>(),Arg.Any<string>(),Arg.Any<string>())
				.Returns("Ascending"); 
			engine.Analyzer.Analyze(AnalysisType.DetectFallingEdges, userDialog);

			var flaggedRecords = engine
				.Filter.Results
				.Count(x => x.Metadata.IsFlagged);

                        // 8 transitions + 1 for the first value found
                        Assert.AreEqual(9, flaggedRecords);
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
