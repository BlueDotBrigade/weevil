namespace BlueDotBrigade.Weevil.StepDefinitions;

/// <summary>
/// Opens a copy of a test log, so a scenario can change the log while it is open without changing the test data.
/// </summary>
[Binding]
internal sealed class ChangingLogSteps : ReqnrollSteps
{
	private string _folder = string.Empty;
	private string _logPath = string.Empty;

	public ChangingLogSteps(Token token) : base(token)
	{
		// nothing to do
	}

	/// <remarks>
	/// Beside the tests, not in the temporary folder, where a log is treated as extracted from an archive.
	/// </remarks>
	[Given($@"that a copy of the {X.FileName} log file is open")]
	public void GivenThatACopyOfTheLogFileIsOpen(string fileName)
	{
		_folder = Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, nameof(ChangingLogSteps), Guid.NewGuid().ToString("N"))).FullName;
		_logPath = Path.Combine(_folder, fileName);
		File.Copy(new Daten().AsFilePath(fileName), _logPath);

		this.Context.Engine = Engine.UsingPath(_logPath).Open();
	}

	[When("this record is appended to the log:")]
	public void WhenThisRecordIsAppendedToTheLog(string record)
	{
		File.AppendAllText(_logPath, record + "\n");
	}

	[AfterScenario]
	public void DeleteTheCopy()
	{
		if (Directory.Exists(_folder))
		{
			Directory.Delete(_folder, recursive: true);
		}
	}
}
