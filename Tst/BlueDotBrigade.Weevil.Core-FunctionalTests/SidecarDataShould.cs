namespace BlueDotBrigade.Weevil
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Xml.Linq;
	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class SidecarDataShould
	{
		[TestMethod]
		public void UseLineNumberWhenLoadingUserComment()
		{
			IEngine engine = Engine
				.UsingPath(new Daten().AsFilePath("UseLineNumberWhenLoadingUserComment.log"))
				.Open();

			engine.Selector.Select(31);

			engine.Clear(ClearOperation.BeforeSelected);

			engine.Filter.Results[1].LineNumber.Should().Be(32);
			engine.RecordState.GetComment(engine.Filter.Results[1]).Should().Be("Index31");
		}

		[TestMethod]
		public void KeepCommentsForRecordsNoLongerInMemory()
		{
			IEngine firstEngine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			firstEngine.RecordState.GetComment(firstEngine[0]).Should().Be("First");

			firstEngine.Selector.Select(5);
			firstEngine.Clear(ClearOperation.BeforeSelected);
			firstEngine.Save(true);
			firstEngine = null;

			IEngine secondEngine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();

			secondEngine.RecordState.GetComment(secondEngine[0]).Should().Be("First");
		}

		// Issue #977: saving the sidecar writes byte offsets taken from the line index.
		[TestMethod]
		public void GivenCommentAndBookmark_WhenSaving_ThenSidecarHasTheirByteOffsets()
		{
			// Not under the temporary folder, where files are treated as extracted from a compressed archive and nothing is saved.
			var directory = Path.Combine(AppContext.BaseDirectory, nameof(SidecarDataShould), Guid.NewGuid().ToString("N"));
			var logFilePath = Path.Combine(directory, "Application.log");
			Directory.CreateDirectory(directory);

			try
			{
				// Every line is 9 bytes, so line N begins at byte (N - 1) * 9.
				File.WriteAllText(logFilePath, string.Concat(Enumerable.Range(1, 300).Select(lineNumber => $"Line {lineNumber:000}\n")));

				IEngine engine = Engine.UsingPath(logFilePath).Open();
				engine.RecordState.SetComment(engine[149], "A comment on line 150");
				engine.Bookmarks.Create(1, "A bookmark on line 180", 180);
				engine.Save(true);

				XElement data = XDocument.Load($"{logFilePath}.xml").Root.Elements().First(e => e.Name.LocalName == "CommonData");
				XElement Child(XElement parent, string name) => parent.Elements().First(e => e.Name.LocalName == name);

				Child(Child(Child(Child(data, "Records"), "Record"), "RelatesTo"), "ByteOffset").Value.Should().Be("1341");
				Child(Child(Child(Child(data, "Bookmarks"), "Bookmark"), "Record"), "ByteOffset").Value.Should().Be("1611");
				File.Exists($"{logFilePath}.idx").Should().BeTrue();
			}
			finally
			{
				Directory.Delete(directory, recursive: true);
			}
		}

		[TestMethod]
		[WorkItem(1004)]
		public void GivenUnsavedComment_WhenItsRecordIsClearedAndSaved_ThenSidecarKeepsTheComment()
		{
			// Comments and pins on cleared records were lost when the sidecar was saved.
			var logFilePath = CreateLogFile(out var directory);

			try
			{
				IEngine engine = Engine.UsingPath(logFilePath).Open();
				engine.RecordState.SetComment(engine[11], "hello world");
				ClearLinesOneToTwelve(engine);
				engine.Save(true);

				IEngine reopened = Engine.UsingPath(logFilePath).Open();
				reopened.RecordState.GetComment(reopened[11]).Should().Be("hello world");
			}
			finally
			{
				Directory.Delete(directory, recursive: true);
			}
		}

		[TestMethod]
		[WorkItem(1004)]
		public void GivenSavedComment_WhenEditedThenClearedAndSaved_ThenSidecarHasTheEdit()
		{
			// Comments and pins on cleared records were lost when the sidecar was saved.
			var logFilePath = CreateLogFile(out var directory);

			try
			{
				IEngine engine = Engine.UsingPath(logFilePath).Open();
				engine.RecordState.SetComment(engine[11], "before");
				engine.Save(true);

				engine.RecordState.SetComment(engine[11], "after");
				ClearLinesOneToTwelve(engine);
				engine.Save(true);

				IEngine reopened = Engine.UsingPath(logFilePath).Open();
				reopened.RecordState.GetComment(reopened[11]).Should().Be("after");
			}
			finally
			{
				Directory.Delete(directory, recursive: true);
			}
		}

		[TestMethod]
		[WorkItem(1004)]
		public void GivenSavedPin_WhenUnpinnedThenClearedAndSaved_ThenSidecarHasNoPin()
		{
			// Comments and pins on cleared records were lost when the sidecar was saved.
			var logFilePath = CreateLogFile(out var directory);

			try
			{
				IEngine engine = Engine.UsingPath(logFilePath).Open();
				engine.RecordState.SetPinned(engine[11], true);
				engine.Save(true);

				engine.RecordState.SetPinned(engine[11], false);
				ClearLinesOneToTwelve(engine);
				engine.Save(true);

				IEngine reopened = Engine.UsingPath(logFilePath).Open();
				reopened.RecordState.IsPinned(reopened[11]).Should().BeFalse();
			}
			finally
			{
				Directory.Delete(directory, recursive: true);
			}
		}

		/// <summary>
		/// Writes a 300-line log, one record per line, outside the temporary folder, where the sidecar would not be saved.
		/// </summary>
		private static string CreateLogFile(out string directory)
		{
			directory = Path.Combine(AppContext.BaseDirectory, nameof(SidecarDataShould), Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(directory);

			var logFilePath = Path.Combine(directory, "Application.log");
			File.WriteAllText(logFilePath, string.Concat(Enumerable.Range(1, 300).Select(lineNumber => $"Line {lineNumber:000}\n")));

			return logFilePath;
		}

		private static void ClearLinesOneToTwelve(IEngine engine)
		{
			engine.Selector.Select(13);
			engine.Clear(ClearOperation.BeforeSelected);
			engine.Filter.Results[0].LineNumber.Should().Be(13);
		}
	}
}
