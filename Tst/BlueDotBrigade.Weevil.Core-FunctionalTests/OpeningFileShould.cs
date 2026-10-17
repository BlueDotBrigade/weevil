
namespace BlueDotBrigade.Weevil
{
	using System;
	using BlueDotBrigade.Weevil.Filter;

	[TestClass]
	public class OpeningFileShould
	{
		[TestMethod]
		public void OpenEmptyFile()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("EmptyFile.txt"))
				.Open();

			engine.Count.Should().Be(0);
		}

		[TestMethod]
		public void OpenFileWithOnlyWhitespace()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("FileWithOnlyWhitespace.txt"))
				.Open();

			engine.Count.Should().Be(1);
		}

		// Issue #959: a header with a number too large to parse is treated as part of the previous record.
		[TestMethod]
		public void GivenTsvHeaderWithOversizedProcessId_WhenOpening_ThenLoadingContinues()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("TsvLogWithOversizedProcessId.log"))
				.Open();

			engine.Count.Should().Be(2);
			engine[0].Content.Should().EndWith("Process ID is too large");
			engine[1].LineNumber.Should().Be(3);
		}

		[TestMethod]
		public void LoadStartingAtLineNumber()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault), startAtLineNumber: 100)
				.Open();

			engine[0].LineNumber.Should().Be(100);
			engine[0].Content.Should().Be("Info 1900-01-01 12:01:40.0000 248 Application.Controller: Changing state from old state NoSession to new state Section100.");
		}

		// Issue #958: sidecar byte offsets are unreliable, so they must not decide where loading starts.
		private const string LogWithUnreliableSidecarOffsets = "LogWithUnreliableSidecarOffsets.log";

		[TestMethod]
		public void GivenNoSidecarSectionOnStartLine_WhenOpeningAtLineNumber_ThenFirstRecordIsThatLine()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(LogWithUnreliableSidecarOffsets), startAtLineNumber: 120)
				.Open();

			engine[0].LineNumber.Should().Be(120);
			engine[0].Content.Should().Be("Line 120");
		}

		[TestMethod]
		public void GivenSidecarOffsetIsZero_WhenOpeningAtLineNumber_ThenFirstRecordIsThatLine()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(LogWithUnreliableSidecarOffsets), startAtLineNumber: 100)
				.Open();

			engine[0].LineNumber.Should().Be(100);
			engine[0].Content.Should().Be("Line 100");
		}

		[TestMethod]
		public void GivenSidecarOffsetIsNegative_WhenOpeningAtLineNumber_ThenFirstRecordIsThatLine()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(LogWithUnreliableSidecarOffsets), startAtLineNumber: 150)
				.Open();

			engine[0].LineNumber.Should().Be(150);
			engine[0].Content.Should().Be("Line 150");
		}

		[TestMethod]
		public void GivenSidecarOffsetIsMidLine_WhenOpeningAtLineNumber_ThenFirstRecordIsThatLine()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(LogWithUnreliableSidecarOffsets), startAtLineNumber: 180)
				.Open();

			engine[0].LineNumber.Should().Be(180);
			engine[0].Content.Should().Be("Line 180");
		}

		// Issue #977: an offset at the start of the wrong line could not be detected without a line index.
		[TestMethod]
		public void GivenSidecarOffsetIsStartOfAnotherLine_WhenOpeningAtLineNumber_ThenFirstRecordIsThatLine()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(LogWithUnreliableSidecarOffsets), startAtLineNumber: 200)
				.Open();

			engine[0].LineNumber.Should().Be(200);
			engine[0].Content.Should().Be("Line 200");
		}

		[TestMethod]
		public void GivenUnreliableSidecarOffset_WhenOpeningLineRange_ThenOnlyThatRangeIsLoaded()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath(LogWithUnreliableSidecarOffsets), startAtLineNumber: 100)
				.UsingRange(new Range(100, 109))
				.Open();

			engine.Count.Should().Be(10);
			engine[0].Content.Should().Be("Line 100");
			engine[9].LineNumber.Should().Be(109);
			engine[9].Content.Should().Be("Line 109");
		}

		[TestMethod]
		public void LoadUsingSidecarContext()
		{
			var fileFormatKey = "FileFormat";

			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("LogWithSidecarContext.log"))
				.Open();

			engine.Context[fileFormatKey].Should().Be("1.2.3.4");
		}

		[TestMethod]
		public void LoadUsingExplicitContext()
		{
			var fileFormatKey = "FileFormat";
			var fileFormatValue = new Version(512, 512).ToString();

			var context = new ContextDictionary
			{
				{ fileFormatKey, fileFormatValue},
			};

			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("LogWithSidecarContext.log"))
				.UsingContext(context)
				.Open();

			engine.Context[fileFormatKey].Should().Be(fileFormatValue);
		}
	}
}