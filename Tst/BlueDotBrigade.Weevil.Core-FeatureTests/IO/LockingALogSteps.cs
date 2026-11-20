namespace BlueDotBrigade.Weevil.StepDefinitions;

using LogInUseException = BlueDotBrigade.Weevil.IO.LogInUseException;

/// <summary>
/// Opens a copy of the default log while another Weevil has it open, or after (#1254).
/// </summary>
/// <remarks>
/// Another engine in this process stands for the other Weevil: Weevil refuses a second engine of an open log as it refuses another instance.
/// </remarks>
[Binding]
internal sealed class LockingALogSteps : ReqnrollSteps
{
	private string _folder = string.Empty;
	private string _logPath = string.Empty;
	private IEngine _otherWeevil = Engine.Surrogate;
	private Exception? _openFailure;

	public LockingALogSteps(Token token) : base(token)
	{
		// nothing to do
	}

	private string LockPath => $"{_logPath}.lock";

	[Given("that a copy of the default log file is open")]
	public void GivenThatACopyOfTheDefaultLogFileIsOpen()
	{
		CopyTheDefaultLog();
		this.Context.Engine = Engine.UsingPath(_logPath).Open();
	}

	[Given("that a copy of the default log file is open in another Weevil")]
	public void GivenThatACopyOfTheDefaultLogFileIsOpenInAnotherWeevil()
	{
		CopyTheDefaultLog();
		_otherWeevil = Engine.UsingPath(_logPath).Open();
	}

	[Given("that a copy of the default log file has a lock beside it that no Weevil holds")]
	public void GivenThatACopyOfTheDefaultLogFileHasALockBesideItThatNoWeevilHolds()
	{
		// As a Weevil that crashed leaves it on Linux: Windows deletes it once the process is gone.
		CopyTheDefaultLog();
		File.WriteAllText(this.LockPath, "User=someone\nComputer=elsewhere\n");
	}

	[Given("that a copy of the default log file has a folder beside it with its lock's name")]
	public void GivenThatACopyOfTheDefaultLogFileHasAFolderBesideItWithItsLocksName()
	{
		// The lock can't be created there, even by an administrator, as in a folder Weevil can't write to.
		CopyTheDefaultLog();
		Directory.CreateDirectory(this.LockPath);
	}

	[Given("the other Weevil has closed it")]
	[When("the other Weevil closes it")]
	public void WhenTheOtherWeevilClosesIt()
	{
		_otherWeevil.Dispose();
	}

	[When("the copy is opened")]
	public void WhenTheCopyIsOpened()
	{
		try
		{
			this.Context.Engine = Engine.UsingPath(_logPath).Open();
		}
		catch (LogInUseException exception)
		{
			_openFailure = exception;
		}
	}

	[When("it is reloaded")]
	public void WhenItIsReloaded()
	{
		this.Context.Engine.Reload();
	}

	[When("it is cleared")]
	public void WhenItIsCleared()
	{
		this.Context.Engine.Selector.Select(lineNumber: 11);
		this.Context.Engine.Clear(ClearOperation.Unselected);
	}

	[Then("the log will not open, and the reason will name this user and this computer")]
	public void ThenTheLogWillNotOpenAndTheReasonWillNameThisUserAndThisComputer()
	{
		LogInUseException refusal = _openFailure.Should().BeOfType<LogInUseException>().Subject;
		refusal.UserName.Should().Be(Environment.UserName);
		refusal.ComputerName.Should().Be(Environment.MachineName);
		refusal.Message.Should().Be($"{Path.GetFileName(_logPath)} is already open in Weevil by {Environment.UserName} on {Environment.MachineName}.");
	}

	[Then("the copy will have no lock beside it")]
	public void ThenTheCopyWillHaveNoLockBesideIt()
	{
		File.Exists(this.LockPath).Should().BeFalse();
	}

	[Then("it will show the records of the copy")]
	public void ThenItWillShowTheRecordsOfTheCopy()
	{
		_openFailure.Should().BeNull();
		Engine.IsRealInstance(this.Context.Engine).Should().BeTrue();
		this.Context.Engine.Count.Should().BeGreaterThan(0);
	}

	[Then("its lock will name this user and this computer")]
	public void ThenItsLockWillNameThisUserAndThisComputer()
	{
		using var lockFile = new FileStream(this.LockPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
		var text = new StreamReader(lockFile).ReadToEnd();

		text.Should().Contain(Environment.UserName).And.Contain(Environment.MachineName);
	}

	[Then("another Weevil will not open the copy")]
	public void ThenAnotherWeevilWillNotOpenTheCopy()
	{
		FluentActions.Invoking(() => Engine.UsingPath(_logPath).Open())
			.Should().Throw<LogInUseException>();
	}

	[AfterScenario]
	public void DeleteTheCopy()
	{
		// The locks are in the folder, so the logs are closed first.
		_otherWeevil.Dispose();
		this.Context.Engine.Dispose();

		if (Directory.Exists(_folder))
		{
			Directory.Delete(_folder, recursive: true);
		}
	}

	private void CopyTheDefaultLog()
	{
		_folder = Directory.CreateTempSubdirectory("Weevil.LockingALog.").FullName;
		_logPath = Path.Combine(_folder, "Droid.log");
		File.Copy(new Daten().AsFilePath(From.GlobalDefault), _logPath);
	}
}
