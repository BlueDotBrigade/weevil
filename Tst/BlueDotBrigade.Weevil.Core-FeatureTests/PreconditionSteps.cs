namespace BlueDotBrigade.Weevil.StepDefinitions;

using System.Linq.Expressions;
using BlueDotBrigade.Weevil.Data;
using BlueDotBrigade.Weevil.Filter;
using BlueDotBrigade.Weevil.Filter.Expressions.PlainText;

[Binding]
internal sealed class PreconditionSteps : ReqnrollSteps
{
	public PreconditionSteps(Token token) : base(token)
	{
		// nothing to do
	}	

	[Given(@"that the default log file is open")]
	public void GivenThatTheDefaultLogFileIsOpen()
	{
		var filePath = new Daten().AsFilePath(From.GlobalDefault);

		this.Context.Engine = Engine
			.UsingPath(filePath)
			.Open();
	}

	[Given(@$"that the {X.FileName} log file name is open")]
	public void GivenThatTheLogFileNameIsOpen(string fileName)
	{
		var filePath = new Daten().AsFilePath(fileName);

		this.Context.Engine = Engine
			.UsingPath(filePath)
			.Open();
	}

	[Given($@"that the log file is open at `{X.FilePath}`")]
	public void GivenThatTheLogFileIsOpenAt(string filePath)
	{
		this.Context.Engine = Engine
			.UsingPath(filePath)
			.Open();
	}
}
