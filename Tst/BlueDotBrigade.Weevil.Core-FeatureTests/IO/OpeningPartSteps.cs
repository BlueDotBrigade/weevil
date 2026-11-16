namespace BlueDotBrigade.Weevil.StepDefinitions;

using System.Security;
using BlueDotBrigade.Weevil.Data;
using StorageMode = BlueDotBrigade.Weevil.IO.StorageMode;

/// <summary>
/// Opens one region or one section of a log, from the regions and sections its sidecar lists (#1213, #1212).
/// </summary>
[Binding]
internal sealed class OpeningPartSteps : ReqnrollSteps
{
	private readonly List<string> _regionElements = new List<string>();
	private readonly List<string> _sectionElements = new List<string>();
	private string _folder = string.Empty;
	private string _logPath = string.Empty;
	private Exception? _openFailure;

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

	[When(@"its ""(.+)"" (region|section) is opened in (RAM|Disk)")]
	public void WhenItsPartIsOpenedIn(string name, string part, string mode)
	{
		IEngineBuilder builder = Engine
			.UsingPath(_logPath)
			.UsingStorage(mode == "Disk" ? StorageMode.Disk : StorageMode.Ram);

		Open(part == "region" ? builder.UsingRegion(name) : builder.UsingSection(name));
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

		IEngine wholeLog = Engine.UsingPath(_logPath).Open();
		string[] expected = Describe(wholeLog.Records).Where(record => record.LineNumber >= firstLine && record.LineNumber <= lastLine).Select(record => record.Text).ToArray();

		expected.Should().NotBeEmpty();
		Describe(this.Context.Engine.Records).Select(record => record.Text).Should().Equal(expected);
	}

	[Then($"it will show the records from line {X.WholeNumber} to the end of the whole log, and no others")]
	public void ThenItWillShowTheRecordsFromLineToTheEndOfTheWholeLogAndNoOthers(int firstLine)
	{
		ThenItWillShowTheRecordsOfLinesOfTheWholeLogAndNoOthers(firstLine, int.MaxValue);
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
