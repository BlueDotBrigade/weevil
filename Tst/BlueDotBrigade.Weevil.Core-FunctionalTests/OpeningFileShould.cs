
namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Filter;
	using NSubstitute;
	using StorageMode = BlueDotBrigade.Weevil.IO.StorageMode;

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

		// The saved line index checks only the first and last 4 KB of the log, so it still matches a log changed in the middle.
		// It keeps the offset of every 64th line, so opening at line 200, after a change on line 100, would show another line.
		[TestMethod]
		public void GivenALogChangedInTheMiddleWhileOpenOnDisk_WhenReloaded_ThenOpeningItAtALaterLineShowsThatLine()
		{
			// Reload after a change rebuilds the saved line index (#1025).
			(var folder, var logPath, var lines) = CreateLogBesideTheTests();

			try
			{
				IEngine engine = new EngineBuilder(logPath).UsingStorage(StorageMode.Disk).Open();
				File.Exists($"{logPath}.idx").Should().BeTrue("the log has a saved line index");

				BreakLine(logPath, lines, lineNumber: 100);
				FluentActions.Invoking(() => engine.Records[99].Content).Should().Throw<IO.LogFileChangedException>();
				engine.Reload();

				IEngine reopened = Engine.UsingPath(logPath, startAtLineNumber: 200).Open();
				reopened[0].LineNumber.Should().Be(200);
				reopened[0].Content.Should().Be(lines[198], "the changed log's line 200 was its line 199");
			}
			finally
			{
				Directory.Delete(folder, recursive: true);
			}
		}

		[TestMethod]
		[DoNotParallelize]
		public void GivenALogChangedInTheMiddleWhileItIsReloadedOnDisk_WhenReloadedAgain_ThenOpeningItAtALaterLineShowsThatLine()
		{
			// The log changes after Reload has read it and saved its line index, before the filter is applied: that index is not kept (#1025).
			(var folder, var logPath, var lines) = CreateLogBesideTheTests();
			ILogWriter original = Log.Default;

			try
			{
				IEngine engine = new EngineBuilder(logPath).UsingStorage(StorageMode.Disk).Open();
				File.Exists($"{logPath}.idx").Should().BeTrue("the log has a saved line index");
				engine.Filter.Apply(FilterType.PlainText, new FilterCriteria("Line"));

				ILogWriter writer = Substitute.For<ILogWriter>();
				writer
					.When(w => w.Write(Arg.Any<LogSeverityType>(), "File loading complete.", Arg.Any<IEnumerable<KeyValuePair<string, object>>>()))
					.Do(_ => BreakLine(logPath, lines, lineNumber: 100));
				Log.Register(writer);
				FluentActions.Invoking(engine.Reload).Should().Throw<IO.LogFileChangedException>();
				Log.Register(original);

				engine.Reload();

				IEngine reopened = Engine.UsingPath(logPath, startAtLineNumber: 200).Open();
				reopened[0].LineNumber.Should().Be(200);
				reopened[0].Content.Should().Be(lines[198], "the changed log's line 200 was its line 199");
			}
			finally
			{
				Log.Register(original);
				Directory.Delete(folder, recursive: true);
			}
		}

		/// <summary>
		/// Writes a log of 300 lines of 54 bytes, so line 100 is past the first 4 KB and before the last 4 KB.
		/// </summary>
		/// <remarks>
		/// A log in the temporary folder gets no saved line index, so this one is kept beside the tests.
		/// </remarks>
		private static (string Folder, string LogPath, string[] Lines) CreateLogBesideTheTests()
		{
			var folder = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, nameof(OpeningFileShould), Guid.NewGuid().ToString("N"))).FullName;
			var logPath = Path.Combine(folder, "Log.txt");
			var lines = Enumerable.Range(1, 300).Select(lineNumber => $"Info 1900-01-01 12:00:00.0000 248 Line {lineNumber:D3} of the log").ToArray();
			File.WriteAllText(logPath, string.Join("\n", lines) + "\n");

			return (folder, logPath, lines);
		}

		/// <summary>
		/// Turns the first space on a line into a line break: the log keeps its length, and every later line moves down one.
		/// </summary>
		private static void BreakLine(string logPath, string[] lines, int lineNumber)
		{
			var bytes = File.ReadAllBytes(logPath);
			var lineStart = Encoding.UTF8.GetByteCount(string.Join("\n", lines.Take(lineNumber - 1)) + "\n");
			bytes[Array.IndexOf(bytes, (byte)' ', lineStart)] = (byte)'\n';
			File.WriteAllBytes(logPath, bytes);
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