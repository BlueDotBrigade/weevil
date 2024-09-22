namespace BlueDotBrigade.Weevil.StepDefinitions
{
	using System.Linq.Expressions;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.Filter.Expressions.PlainText;

	[Binding]
	internal sealed class PreconditionSteps : ReqnrollSteps
	{
		public PreconditionSteps(Token token) : base(token)
		{
		}	

		[Given(@"that the default log file is open")]
		public void GivenThatTheDefaultLogFileIsOpen()
		{
			this.Token.Engine = Engine
				.UsingPath(new Daten().AsFilePath(From.GlobalDefault))
				.Open();
		}

		[Given($@"that the `{X.FileName}` log file is open")]
		public void GivenThatTheLogFileIsOpen(string fileName)
		{
			this.Token.Engine = Engine
				.UsingPath(new Daten().AsFilePath(fileName))
				.Open();
		}

		[Given($@"that the log file is open at `{X.FilePath}`")]
		public void GivenThatTheLogFileIsOpenAt(string filePath)
		{
			this.Token.Engine = Engine
				.UsingPath(filePath)
				.Open();
		}

		[When($@"the inclusive filter is applied `{X.AnyText}`")]
		public void WhenTheInclusiveFilterIsApplied(string inclusiveFilter)
		{
			this.Token.Results = this.Token.Engine.Filter.Apply(
				this.Token.FilterType,
				new FilterCriteria(inclusiveFilter, string.Empty, this.Token.FilterParameters)).Results;
		}
	}
}