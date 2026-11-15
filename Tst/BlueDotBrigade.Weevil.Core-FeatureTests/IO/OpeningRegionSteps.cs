namespace BlueDotBrigade.Weevil.StepDefinitions;

using System.Security;
using BlueDotBrigade.Weevil.Data;
using StorageMode = BlueDotBrigade.Weevil.IO.StorageMode;

[Binding]
internal sealed class OpeningRegionSteps : ReqnrollSteps
{
	private string _folder = string.Empty;
	private string _logPath = string.Empty;
	private Exception? _openFailure;

	public OpeningRegionSteps(Token token) : base(token)
	{
		// nothing to do
	}

	/// <remarks>
	/// Beside the tests, as in the reopening scenarios, with a sidecar written by the test that lists only these regions.
	/// </remarks>
	[Given("that the default log file has been saved beside the tests with these regions in its sidecar:")]
	public void GivenThatTheDefaultLogFileHasBeenSavedBesideTheTestsWithTheseRegionsInItsSidecar(DataTable regions)
	{
		_folder = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, nameof(OpeningRegionSteps), Guid.NewGuid().ToString("N"))).FullName;
		_logPath = Path.Combine(_folder, "Droid.log");
		File.Copy(new Daten().AsFilePath(From.GlobalDefault), _logPath);

		var regionElements = string.Concat(regions.Rows.Select(row =>
			$"<Region><Name>{SecurityElement.Escape(row["Region"])}</Name>" +
			$"<Minimum><ByteOffset>-1</ByteOffset><LineNumber>{row["First Line"]}</LineNumber></Minimum>" +
			$"<Maximum><ByteOffset>-1</ByteOffset><LineNumber>{row["Last Line"]}</LineNumber></Maximum></Region>"));

		File.WriteAllText($"{_logPath}.xml",
			"<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
			"<WeevilSidecar xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns=\"http://schemas.datacontract.org/2004/07/BlueDotBrigade.Weevil.Configuration.Sidecar.v2\">" +
			$"<CommonData><Regions>{regionElements}</Regions><Context/><FilterHistory><ExclusiveFilters/><InclusiveFilters/></FilterHistory><Records/><TableOfContents/><UserRemarks/></CommonData>" +
			"<Header><SavedAt>2026-11-15T00:00:00</SavedAt><SchemaVersion xmlns:d3p1=\"http://schemas.datacontract.org/2004/07/System\">" +
			"<d3p1:_Build>0</d3p1:_Build><d3p1:_Major>4</d3p1:_Major><d3p1:_Minor>2</d3p1:_Minor><d3p1:_Revision>-1</d3p1:_Revision></SchemaVersion></Header>" +
			"<PluginData i:nil=\"true\" /></WeevilSidecar>");
	}

	[When(@"its ""(.+)"" region is opened in (RAM|Disk)")]
	public void WhenItsRegionIsOpenedIn(string regionName, string mode)
	{
		try
		{
			this.Context.Engine = Engine
				.UsingPath(_logPath)
				.UsingStorage(mode == "Disk" ? StorageMode.Disk : StorageMode.Ram)
				.UsingRegion(regionName)
				.Open();
		}
		catch (KeyNotFoundException exception)
		{
			_openFailure = exception;
		}
	}

	[Then($"it will show the records of lines {X.WholeNumber} to {X.WholeNumber} of the whole log, and no others")]
	public void ThenItWillShowTheRecordsOfLinesOfTheWholeLogAndNoOthers(int firstLine, int lastLine)
	{
		IEngine wholeLog = Engine.UsingPath(_logPath).Open();
		string[] expected = Describe(wholeLog.Records).Where(record => record.LineNumber >= firstLine && record.LineNumber <= lastLine).Select(record => record.Text).ToArray();

		expected.Should().NotBeEmpty();
		Describe(this.Context.Engine.Records).Select(record => record.Text).Should().Equal(expected);
	}

	[Then("the log will not open, and the reason will name the regions (.+) and (.+)")]
	public void ThenTheLogWillNotOpenAndTheReasonWillNameTheRegions(string firstRegion, string secondRegion)
	{
		_openFailure.Should().NotBeNull();
		_openFailure!.Message.Should().Contain($"`{firstRegion}`").And.Contain($"`{secondRegion}`");
	}

	[AfterScenario]
	public void DeleteTheSavedLogFile()
	{
		if (Directory.Exists(_folder))
		{
			Directory.Delete(_folder, recursive: true);
		}
	}

	private static IEnumerable<(int LineNumber, string Text)> Describe(RecordList records)
	{
		return records.Select(record => (record.LineNumber, $"{record.LineNumber}|{record.CreatedAt:O}|{record.Severity}|{record.IsMultiLine}|{record.Content}"));
	}
}
