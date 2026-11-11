namespace BlueDotBrigade.Weevil.StepDefinitions;

using System.Text;
using BlueDotBrigade.Weevil.Data;
using StorageMode = BlueDotBrigade.Weevil.IO.StorageMode;

[Binding]
internal sealed class OpeningOnDiskSteps : ReqnrollSteps
{
	private const string Encodings = @"(UTF-8|UTF-8 with BOM|UTF-16 LE|UTF-16 BE|UTF-32 LE|UTF-32 BE)";
	private const string LineEndings = @"(CRLF|LF|CR)";

	private string _folder = string.Empty;
	private string _logPath = string.Empty;

	public OpeningOnDiskSteps(Token token) : base(token)
	{
		// nothing to do
	}

	/// <remarks>
	/// The log is written when the test runs, so its bytes are the same on every computer, whatever Git does to line endings.
	/// UTF-16 and UTF-32 are known only by their byte order mark, so they get one. In the temporary folder, no sidecar or line index is saved.
	/// </remarks>
	[Given($"that the default log file has been saved as {Encodings} with {LineEndings} line endings")]
	public void GivenThatTheDefaultLogFileHasBeenSavedAs(string encodingName, string lineEndingName)
	{
		Encoding encoding = encodingName switch
		{
			"UTF-8" => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
			"UTF-8 with BOM" => new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
			"UTF-16 LE" => new UnicodeEncoding(bigEndian: false, byteOrderMark: true),
			"UTF-16 BE" => new UnicodeEncoding(bigEndian: true, byteOrderMark: true),
			"UTF-32 LE" => new UTF32Encoding(bigEndian: false, byteOrderMark: true),
			_ => new UTF32Encoding(bigEndian: true, byteOrderMark: true),
		};

		var lineEnding = lineEndingName switch
		{
			"CRLF" => "\r\n",
			"LF" => "\n",
			_ => "\r",
		};

		var lines = File.ReadAllLines(new Daten().AsFilePath(From.GlobalDefault));
		var text = string.Join(lineEnding, lines) + lineEnding;

		_folder = Directory.CreateTempSubdirectory("Weevil.OpeningOnDisk.").FullName;
		_logPath = Path.Combine(_folder, "Droid.log");
		File.WriteAllBytes(_logPath, encoding.GetPreamble().Concat(encoding.GetBytes(text)).ToArray());
	}

	[Given("that the saved log file is open on disk")]
	public void GivenThatTheSavedLogFileIsOpenOnDisk()
	{
		this.Context.Engine = new EngineBuilder(_logPath)
			.UsingStorage(StorageMode.Disk)
			.Open();
	}

	/// <remarks>
	/// A log on disk is read whenever a record's content is needed, so a step that reads content would now fail.
	/// </remarks>
	[Given("that the saved log file has been deleted")]
	public void GivenThatTheSavedLogFileHasBeenDeleted()
	{
		File.Delete(_logPath);
	}

	[Then("the log on disk will show the same records and filter results as in RAM")]
	public void ThenTheLogOnDiskWillShowTheSameRecordsAndFilterResultsAsInRam()
	{
		IEngine inRam = new EngineBuilder(_logPath)
			.UsingStorage(StorageMode.Ram)
			.Open();
		inRam.Filter.Apply(this.Context.FilterType, this.Context.FilterCriteria);

		IEngine onDisk = this.Context.Engine;

		Describe(onDisk.Filter.Results).Should().NotBeEmpty("the filter keeps some of the log's records");
		Describe(onDisk.Records).Should().Equal(Describe(inRam.Records));
		Describe(onDisk.Filter.Results).Should().Equal(Describe(inRam.Filter.Results));
	}

	[AfterScenario]
	public void DeleteTheSavedLogFile()
	{
		if (Directory.Exists(_folder))
		{
			Directory.Delete(_folder, recursive: true);
		}
	}

	private static string[] Describe(RecordList records)
	{
		return records.Select(record => $"{record.LineNumber}: {record.Content}").ToArray();
	}
}
