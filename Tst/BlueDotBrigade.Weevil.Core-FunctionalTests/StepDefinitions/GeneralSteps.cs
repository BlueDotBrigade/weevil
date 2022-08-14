namespace BlueDotBrigade.Weevil.StepDefinitions
{
	[Binding]
	public sealed class GeneralSteps
	{
		private IEngine _engine = Engine.Surrogate;

		[Given(@"that Weevil has started")]
		public void GivenThatWeevilHasStarted()
		{
			// nothing to do	
		}

		[When($@"the user opens the `{A.FileName}` file")]
		public void WhenTheUserOpensTheFile(string filename)
		{
			_engine = Engine
				.UsingPath(InputData.GetFilePath(filename))
				.Open();
		}


		[Then($@"the record count shall be {A.RecordCount}")]
		public void ThenTheRecordCountWillBe(int recordCount)
		{
			_engine.Count.Should().Be(recordCount);
		}
	}
}