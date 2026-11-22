namespace BlueDotBrigade.Weevil.StepDefinitions;

using System.Globalization;
using System.Security;
using BlueDotBrigade.Weevil.Data;
using LogPart = BlueDotBrigade.Weevil.IO.LogPart;
using StorageMode = BlueDotBrigade.Weevil.IO.StorageMode;

/// <summary>
/// Opens one region or one section of a log, from the regions and sections its sidecar lists (#1213, #1212), or a range of lines,
/// and reloads that part (#1219).
/// </summary>
[Binding]
internal sealed class OpeningPartSteps : ReqnrollSteps
{
	private readonly List<string> _regionElements = new List<string>();
	private readonly List<string> _sectionElements = new List<string>();
	private string _folder = string.Empty;
	private string _logPath = string.Empty;
	private Exception? _openFailure;
	private Exception? _reloadFailure;
	private IReadOnlyList<LogPart> _parts = Array.Empty<LogPart>();

	public OpeningPartSteps(Token token) : base(token)
	{
		// nothing to do
	}

	/// <remarks>
	/// Beside the tests, as in the reopening scenarios, with a sidecar written by the test that lists only these regions.
	/// </remarks>
	[Given("that the default log file has been saved beside the tests with these regions in its sidecar:")]
	public void GivenThatTheDefaultLogFileHasBeenSavedBesideTheTestsWithTheseRegionsInItsSidecar(DataTable regions)
	{
		_regionElements.AddRange(regions.Rows.Select(row =>
			$"<Region><Name>{SecurityElement.Escape(row["Region"])}</Name>" +
			$"<Minimum><ByteOffset>-1</ByteOffset><LineNumber>{row["First Line"]}</LineNumber></Minimum>" +
			$"<Maximum><ByteOffset>-1</ByteOffset><LineNumber>{row["Last Line"]}</LineNumber></Maximum></Region>"));

		SaveLogWithSidecar();
	}

	/// <remarks>
	/// Beside the tests, with a sidecar written by the test whose table of contents lists only these sections.
	/// </remarks>
	[Given("that the default log file has been saved beside the tests with these sections in its sidecar:")]
	public void GivenThatTheDefaultLogFileHasBeenSavedBesideTheTestsWithTheseSectionsInItsSidecar(DataTable sections)
	{
		_sectionElements.AddRange(sections.Rows.Select(row =>
			$"<Section><Level>{row["Level"]}</Level><Name>{SecurityElement.Escape(row["Section"])}</Name>" +
			$"<RelatesTo><ByteOffset>-1</ByteOffset><LineNumber>{row["Line"]}</LineNumber></RelatesTo></Section>"));

		SaveLogWithSidecar();
	}

	/// <remarks>
	/// Beside the tests, with a sidecar written by the test that lists no regions or sections.
	/// </remarks>
	[Given("that the default log file has been saved beside the tests")]
	public void GivenThatTheDefaultLogFileHasBeenSavedBesideTheTests()
	{
		SaveLogWithSidecar();
	}

	[Given(@"its ""(.+)"" (region|section) has been opened in (RAM|Disk)")]
	[When(@"its ""(.+)"" (region|section) is opened in (RAM|Disk)")]
	public void WhenItsPartIsOpenedIn(string name, string part, string mode)
	{
		IEngineBuilder builder = Engine
			.UsingPath(_logPath)
			.UsingStorage(ToStorageMode(mode));

		Open(part == "region" ? builder.UsingRegion(name) : builder.UsingSection(name));
	}

	[Given($@"it has been opened from line {X.WholeNumber} in (RAM|Disk)")]
	public void GivenItHasBeenOpenedFromLineIn(int firstLine, string mode)
	{
		Open(Engine.UsingPath(_logPath, firstLine).UsingStorage(ToStorageMode(mode)));
	}

	/// <remarks>
	/// From the first line, with a range, as Weevil's window opens the lines a plugin's Open As chooses.
	/// </remarks>
	[Given($@"its lines {X.WholeNumber} to {X.WholeNumber} have been opened in (RAM|Disk)")]
	public void GivenItsLinesHaveBeenOpenedIn(int firstLine, int lastLine, string mode)
	{
		Open(Engine.UsingPath(_logPath, firstLine).UsingRange(new Range(firstLine, lastLine)).UsingStorage(ToStorageMode(mode)));
	}

	[Given($@"the ""(.+)"" region has been changed to run from line {X.WholeNumber} to line {X.WholeNumber}")]
	public void GivenTheRegionHasBeenChangedToRunFromLineToLine(string name, int firstLine, int lastLine)
	{
		GivenTheRegionHasBeenRemoved(name);
		this.Context.Engine.Regions.CreateFromSelection(name, new[] { firstLine, lastLine });
	}

	[Given(@"the ""(.+)"" region has been removed")]
	public void GivenTheRegionHasBeenRemoved(string name)
	{
		Region region = this.Context.Engine.Regions.Regions.Single(r => r.Name == name);
		this.Context.Engine.Regions.Clear(region.Minimum.LineNumber);
	}

	/// <remarks>
	/// The first records of the default log, written again at its end, which has no line ending after its last line.
	/// </remarks>
	[Given("records have been appended to the log")]
	public void GivenRecordsHaveBeenAppendedToTheLog()
	{
		IEnumerable<string> lines = File.ReadAllLines(new Daten().AsFilePath(From.GlobalDefault)).Take(20);
		File.AppendAllText(_logPath, "\n" + string.Join("\n", lines));
	}

	[Given("the log itself has been deleted")]
	public void GivenTheLogItselfHasBeenDeleted()
	{
		File.Delete(_logPath);
	}

	[When("the parts of the log are listed")]
	public void WhenThePartsOfTheLogAreListed()
	{
		_parts = Engine.ReadParts(_logPath);
	}

	[When("the log is reloaded")]
	public void WhenTheLogIsReloaded()
	{
		try
		{
			this.Context.Engine.Reload();
		}
		catch (KeyNotFoundException exception)
		{
			_reloadFailure = exception;
		}
	}

	[When(@"its ""(.+)"" region and its ""(.+)"" section are both opened")]
	public void WhenItsRegionAndItsSectionAreBothOpened(string regionName, string sectionName)
	{
		Open(Engine.UsingPath(_logPath).UsingRegion(regionName).UsingSection(sectionName));
	}

	[Then($"it will show the records of lines {X.WholeNumber} to {X.WholeNumber} of the whole log, and no others")]
	public void ThenItWillShowTheRecordsOfLinesOfTheWholeLogAndNoOthers(int firstLine, int lastLine)
	{
		_openFailure.Should().BeNull();

		// A copy, since the part is open: another open of the log would be refused (#1254).
		var copyPath = Path.Combine(Directory.CreateDirectory(Path.Combine(_folder, "Copy")).FullName, Path.GetFileName(_logPath));
		File.Copy(_logPath, copyPath);

		using IEngine wholeLog = Engine.UsingPath(copyPath).Open();
		string[] expected = Describe(wholeLog.Records).Where(record => record.LineNumber >= firstLine && record.LineNumber <= lastLine).Select(record => record.Text).ToArray();

		expected.Should().NotBeEmpty();
		Describe(this.Context.Engine.Records).Select(record => record.Text).Should().Equal(expected);
	}

	[Then($"it will show the records from line {X.WholeNumber} to the end of the whole log, and no others")]
	public void ThenItWillShowTheRecordsFromLineToTheEndOfTheWholeLogAndNoOthers(int firstLine)
	{
		ThenItWillShowTheRecordsOfLinesOfTheWholeLogAndNoOthers(firstLine, int.MaxValue);
	}

	[Then("these parts will be listed:")]
	public void ThenThesePartsWillBeListed(DataTable expected)
	{
		_parts.Select(Describe).Should().Equal(expected.Rows.Select(row => $"{row["Part"]} {row["Name"]} {row["First Line"]} {row["Last Line"]}"));

		static string Describe(LogPart part)
		{
			var lastLine = part.IsToEndOfLog ? "end" : part.Lines.End.Value.ToString(CultureInfo.InvariantCulture);
			return part.SectionName is not null
				? $"Section {part.SectionName} {part.Lines.Start.Value} {lastLine}"
				: $"Region {part.RegionName} {part.Lines.Start.Value} {lastLine}";
		}
	}

	[Then(@"the part open will be the ""(.+)"" (region|section)")]
	public void ThenThePartOpenWillBeThe(string name, string part)
	{
		(part == "region" ? this.Context.Engine.Part.RegionName : this.Context.Engine.Part.SectionName).Should().Be(name);
	}

	[Then("the log will not reload, and the reason will name: (.+)")]
	public void ThenTheLogWillNotReloadAndTheReasonWillName(string names)
	{
		_reloadFailure.Should().NotBeNull();

		foreach (var name in names.Split(',', StringSplitOptions.TrimEntries))
		{
			_reloadFailure!.Message.Should().Contain($"`{name}`");
		}
	}

	[Then("the log will not open, and the reason will name: (.+)")]
	public void ThenTheLogWillNotOpenAndTheReasonWillName(string names)
	{
		_openFailure.Should().NotBeNull();

		foreach (var name in names.Split(',', StringSplitOptions.TrimEntries))
		{
			_openFailure!.Message.Should().Contain($"`{name}`");
		}
	}

	[AfterScenario]
	public void DeleteTheSavedLogFile()
	{
		// The open log's lock is in the folder, so the log is closed first (#1254).
		this.Context.Engine.Dispose();

		if (Directory.Exists(_folder))
		{
			Directory.Delete(_folder, recursive: true);
		}
	}

	private void Open(IEngineBuilder builder)
	{
		try
		{
			this.Context.Engine = builder.Open();
		}
		catch (Exception exception) when (exception is KeyNotFoundException or InvalidOperationException)
		{
			_openFailure = exception;
		}
	}

	private static StorageMode ToStorageMode(string mode)
	{
		return mode == "Disk" ? StorageMode.Disk : StorageMode.Ram;
	}

	private void SaveLogWithSidecar()
	{
		if (_folder.Length == 0)
		{
			_folder = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, nameof(OpeningPartSteps), Guid.NewGuid().ToString("N"))).FullName;
			_logPath = Path.Combine(_folder, "Droid.log");
			File.Copy(new Daten().AsFilePath(From.GlobalDefault), _logPath);
		}

		File.WriteAllText($"{_logPath}.xml",
			"<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
			"<WeevilSidecar xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.datacontract.org/2004/07/BlueDotBrigade.Weevil.Configuration.Sidecar.v2\">" +
			$"<CommonData><Regions>{string.Concat(_regionElements)}</Regions><Context/><FilterHistory><ExclusiveFilters/><InclusiveFilters/></FilterHistory><Records/>" +
			$"<TableOfContents>{string.Concat(_sectionElements)}</TableOfContents><UserRemarks/></CommonData>" +
			"<Header><SavedAt>2026-11-15T00:00:00</SavedAt><SchemaVersion xmlns:d3p1=\"http://schemas.datacontract.org/2004/07/System\">" +
			"<d3p1:_Build>0</d3p1:_Build><d3p1:_Major>4</d3p1:_Major><d3p1:_Minor>2</d3p1:_Minor><d3p1:_Revision>-1</d3p1:_Revision></SchemaVersion></Header>" +
			"<PluginData i:nil=\"true\" /></WeevilSidecar>");
	}

	private static IEnumerable<(int LineNumber, string Text)> Describe(RecordList records)
	{
		return records.Select(record => (record.LineNumber, $"{record.LineNumber}|{record.CreatedAt:O}|{record.Severity}|{record.IsMultiLine}|{record.Content}"));
	}
}
