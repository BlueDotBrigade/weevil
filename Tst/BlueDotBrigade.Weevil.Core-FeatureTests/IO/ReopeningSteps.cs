namespace BlueDotBrigade.Weevil.StepDefinitions;

using System.Text;
using BlueDotBrigade.Weevil.Data;
using BlueDotBrigade.Weevil.Diagnostics;
using GrownLogParser = BlueDotBrigade.Weevil.IO.GrownLogParser;
using LineIndexFile = BlueDotBrigade.Weevil.IO.LineIndexFile;
using StorageMode = BlueDotBrigade.Weevil.IO.StorageMode;

[Binding]
internal sealed class ReopeningSteps : ReqnrollSteps
{
	private const string Encodings = @"(UTF-8|UTF-8 with BOM|UTF-16 LE|UTF-16 BE|UTF-32 LE|UTF-32 BE)";
	private const string LineEndings = @"(CRLF|LF|CR)";
	private const string Modes = @"(RAM|Disk)";

	private string _folder = string.Empty;
	private string _logPath = string.Empty;
	private Encoding _encoding = Encoding.UTF8;
	private string _lineEnding = "\n";
	private int _startLine = 1;
	private readonly List<string> _messages = new List<string>();

	public ReopeningSteps(Token token) : base(token)
	{
		// nothing to do
	}

	/// <remarks>
	/// Beside the tests, not in the temporary folder: a log there is treated as extracted from an archive, and no index is saved for it.
	/// The log is written when the test runs, so its bytes are the same on every computer, whatever Git does to line endings.
	/// </remarks>
	[Given($"that the default log file has been saved beside the tests as {Encodings} with {LineEndings} line endings")]
	public void GivenThatTheDefaultLogFileHasBeenSavedBesideTheTests(string encodingName, string lineEndingName)
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
		_encoding = encoding;
		_lineEnding = lineEnding;

		_folder = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, nameof(ReopeningSteps), Guid.NewGuid().ToString("N"))).FullName;
		_logPath = Path.Combine(_folder, "Droid.log");
		File.WriteAllBytes(_logPath, encoding.GetPreamble().Concat(encoding.GetBytes(text)).ToArray());
	}

	[Given($"that the saved log file has been opened in {Modes}")]
	public void GivenThatTheSavedLogFileHasBeenOpenedIn(string mode)
	{
		_ = Open(_logPath, mode);
	}

	[Given("one character in the middle of the saved log file has been changed")]
	public void GivenOneCharacterInTheMiddleOfTheSavedLogFileHasBeenChanged()
	{
		// A letter becomes another letter, so the log keeps its length, and its first and last bytes, which the line index checks.
		var bytes = File.ReadAllBytes(_logPath);
		var middle = Array.FindIndex(bytes, bytes.Length / 2, b => b == (byte)'e');
		bytes[middle] = (byte)'f';
		File.WriteAllBytes(_logPath, bytes);
	}

	[Given("a line break has replaced one character in the middle of the saved log file")]
	public void GivenALineBreakHasReplacedOneCharacterInTheMiddleOfTheSavedLogFile()
	{
		// The log keeps its length, and its first and last bytes, which the line index checks, but every line after the change moves down one.
		var bytes = File.ReadAllBytes(_logPath);
		var middle = Array.FindIndex(bytes, bytes.Length / 2, b => b == (byte)' ');
		bytes[middle] = (byte)'\n';
		File.WriteAllBytes(_logPath, bytes);
	}

	/// <remarks>
	/// The first records of the default log, written again at its end, in its encoding and line endings.
	/// </remarks>
	[Given("records have been appended to the saved log file")]
	public void GivenRecordsHaveBeenAppendedToTheSavedLogFile()
	{
		var lines = File.ReadAllLines(new Daten().AsFilePath(From.GlobalDefault)).Take(20);
		Append(string.Join(_lineEnding, lines) + _lineEnding);
	}

	[Given("lines that continue the last record have been appended to the saved log file")]
	public void GivenLinesThatContinueTheLastRecordHaveBeenAppendedToTheSavedLogFile()
	{
		Append($"   at Droid.Navigation.Plot(){_lineEnding}   at Droid.Navigation.Start(){_lineEnding}");
	}

	[Given("one byte of the saved record table has been changed")]
	public void GivenOneByteOfTheSavedRecordTableHasBeenChanged()
	{
		// The rows are the last section before the end marker, whose kind, length and checksum take 20 bytes.
		var indexPath = $"{_logPath}.{LineIndexFile.FileExtension}";
		var bytes = File.ReadAllBytes(indexPath);
		bytes[bytes.Length - 20 - 5] ^= 0x01;
		File.WriteAllBytes(indexPath, bytes);
	}

	[When($"the saved log file is opened again in {Modes}")]
	public void WhenTheSavedLogFileIsOpenedAgainIn(string mode)
	{
		WhenTheSavedLogFileIsOpenedAgainInFromLine(mode, 1);
	}

	[When($@"the saved log file is opened again in {Modes} from line (\d+)")]
	public void WhenTheSavedLogFileIsOpenedAgainInFromLine(string mode, int lineNumber)
	{
		ILogWriter original = Log.Default;
		Log.Register(new CapturingLogWriter(original, _messages));
		_startLine = lineNumber;

		try
		{
			this.Context.Engine = Open(_logPath, mode, lineNumber);
		}
		finally
		{
			Log.Register(original);
		}
	}

	[Then("it will have been opened without being parsed")]
	public void ThenItWillHaveBeenOpenedWithoutBeingParsed()
	{
		_messages.Should().Contain(LineIndexFile.RecordsReadFromIndexMessage);
	}

	[Then("it will have been parsed")]
	public void ThenItWillHaveBeenParsed()
	{
		_messages.Should().NotContain(LineIndexFile.RecordsReadFromIndexMessage);
	}

	[Then("it will have been parsed only in its new part")]
	public void ThenItWillHaveBeenParsedOnlyInItsNewPart()
	{
		_messages.Should().Contain(LineIndexFile.OldRecordsReadFromIndexMessage);
		_messages.Should().Contain(GrownLogParser.NewPartParsedMessage);
		_messages.Should().NotContain(LineIndexFile.RecordsReadFromIndexMessage);
	}

	[Then("its last record will span several lines")]
	public void ThenItsLastRecordWillSpanSeveralLines()
	{
		RecordList records = this.Context.Engine.Records;
		records[records.Count - 1].IsMultiLine.Should().BeTrue();
	}

	[Then("it will have been parsed, because its saved record table does not match it")]
	public void ThenItWillHaveBeenParsedBecauseItsSavedRecordTableDoesNotMatchIt()
	{
		ThenItWillHaveBeenParsed();
		_messages.Should().Contain(message => message.StartsWith(LineIndexFile.TableRejectedMessage));
	}

	/// <remarks>
	/// The parse opens a copy in the temporary folder, where no index is read or saved, in the same mode as the log reopened.
	/// </remarks>
	[Then("it will show the same records as a parse of the log")]
	public void ThenItWillShowTheSameRecordsAsAParseOfTheLog()
	{
		IEngine reopened = this.Context.Engine;
		var copyFolder = Directory.CreateTempSubdirectory("Weevil.Reopening.").FullName;

		try
		{
			var copyPath = Path.Combine(copyFolder, "Droid.log");
			File.Copy(_logPath, copyPath);
			IEngine parsed = Engine.UsingPath(copyPath, _startLine).UsingStorage(reopened.StorageMode).Open();

			Describe(reopened.Records).Should().NotBeEmpty();
			Describe(reopened.Records).Should().Equal(Describe(parsed.Records));
		}
		finally
		{
			Directory.Delete(copyFolder, recursive: true);
		}
	}

	[AfterScenario]
	public void DeleteTheSavedLogFile()
	{
		if (Directory.Exists(_folder))
		{
			Directory.Delete(_folder, recursive: true);
		}
	}

	private static IEngine Open(string logPath, string mode, int startLine = 1)
	{
		return Engine.UsingPath(logPath, startLine)
			.UsingStorage(mode == "Disk" ? StorageMode.Disk : StorageMode.Ram)
			.Open();
	}

	private void Append(string text)
	{
		using var log = new FileStream(_logPath, FileMode.Append, FileAccess.Write);
		log.Write(_encoding.GetBytes(text));
	}

	private static string[] Describe(RecordList records)
	{
		return records
			.Select(record => $"{record.LineNumber}|{record.CreatedAt:O}|{record.Severity}|{record.IsMultiLine}|{record.Content}")
			.ToArray();
	}

	/// <summary>
	/// Keeps the message of every entry written, and passes each on to the writer it replaces.
	/// </summary>
	private sealed class CapturingLogWriter : ILogWriter
	{
		private readonly ILogWriter _next;
		private readonly List<string> _messages;

		public CapturingLogWriter(ILogWriter next, List<string> messages)
		{
			_next = next;
			_messages = messages;
		}

		public void Write(string message) => Write(LogSeverityType.Debug, message, null);

		public void Write(string message, IEnumerable<KeyValuePair<string, object>> metadata) => Write(LogSeverityType.Debug, message, metadata);

		public void Write(LogSeverityType severity, string message) => Write(severity, message, null);

		public void Write(LogSeverityType severity, string message, IEnumerable<KeyValuePair<string, object>> metadata)
		{
			_messages.Add(message);
			_next.Write(severity, message, metadata ?? Array.Empty<KeyValuePair<string, object>>());
		}

		public void Write(LogSeverityType severity, Exception exception) => _next.Write(severity, exception);

		public void Write(LogSeverityType severity, Exception exception, string message)
		{
			_messages.Add(message);
			_next.Write(severity, exception, message);
		}

		public void Write(LogSeverityType severity, Exception exception, string message, IEnumerable<KeyValuePair<string, object>> metadata)
		{
			_messages.Add(message);
			_next.Write(severity, exception, message, metadata);
		}
	}
}
