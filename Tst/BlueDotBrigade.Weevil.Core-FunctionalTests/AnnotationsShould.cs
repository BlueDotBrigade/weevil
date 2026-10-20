namespace BlueDotBrigade.Weevil
{
	using System;
	using System.IO;
	using System.Linq;
	using BlueDotBrigade.Weevil.Data;

	/// <summary>
	/// Comments and pins are kept for the log, and flags for the view, so they carry across Clear and Reload (#1013).
	/// </summary>
	[TestClass]
	public class AnnotationsShould
	{
		private string _directory = string.Empty;

		[TestInitialize]
		public void Setup()
		{
			_directory = Path.Combine(AppContext.BaseDirectory, nameof(AnnotationsShould), Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_directory);
		}

		[TestCleanup]
		public void Cleanup()
		{
			Directory.Delete(_directory, recursive: true);
		}

		[TestMethod]
		public void GivenUnsavedComment_WhenReloaded_ThenTheCommentIsKept()
		{
			IEngine engine = Open();
			Line(engine, 20).Metadata.Comment = "Retry starts here";

			engine.Reload();

			Line(engine, 20).Metadata.Comment.Should().Be("Retry starts here");
			Line(engine, 20).Metadata.IsPinned.Should().BeTrue();
		}

		[TestMethod]
		public void GivenFlaggedRecord_WhenReloaded_ThenItIsStillFlagged()
		{
			IEngine engine = Open();
			Line(engine, 20).Metadata.IsFlagged = true;

			engine.Reload();

			Line(engine, 20).Metadata.IsFlagged.Should().BeTrue();
			Line(engine, 21).Metadata.IsFlagged.Should().BeFalse();
		}

		[TestMethod]
		public void GivenFlaggedAndCommentedRecords_WhenCleared_ThenTheRecordsLeftKeepThem()
		{
			IEngine engine = Open();
			Line(engine, 20).Metadata.IsFlagged = true;
			Line(engine, 21).Metadata.Comment = "Retry starts here";

			engine.Selector.Select(13);
			engine.Clear(ClearOperation.BeforeSelected);

			Line(engine, 20).Metadata.IsFlagged.Should().BeTrue();
			Line(engine, 21).Metadata.Comment.Should().Be("Retry starts here");
		}

		[TestMethod]
		public void GivenRecordClearedThenReloaded_WhenItReturns_ThenItKeepsItsComment()
		{
			IEngine engine = Open();
			Line(engine, 5).Metadata.Comment = "Before the retry";
			engine.Selector.Select(13);
			engine.Clear(ClearOperation.BeforeSelected);

			engine.Reload();

			Line(engine, 5).Metadata.Comment.Should().Be("Before the retry");
		}

		private IEngine Open()
		{
			var logFilePath = Path.Combine(_directory, "Application.log");
			File.WriteAllText(logFilePath, string.Concat(Enumerable.Range(1, 300).Select(lineNumber => $"Line {lineNumber:000}\n")));

			return Engine.UsingPath(logFilePath).Open();
		}

		private static IRecord Line(IEngine engine, int lineNumber)
		{
			return engine.Records.Single(record => record.LineNumber == lineNumber);
		}
	}
}
