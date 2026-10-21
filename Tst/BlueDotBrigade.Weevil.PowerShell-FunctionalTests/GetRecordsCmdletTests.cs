namespace BlueDotBrigade.Weevil.PowerShell
{
	using System.Linq;
	using BlueDotBrigade.Weevil.Data;

	[TestClass]
	public class GetRecordsCmdletTests
	{
		// Regression: Issue #1002
		[TestMethod]
		public void GivenIncludeFilter_WhenGettingRecords_ThenReturnsOneRecordPerMatch()
		{
			var cmdlet = new GetRecordsCmdlet
			{
				FilePath = new Daten().AsFilePath("Application.log"),
				Include = "Error",
			};

			var output = cmdlet.Invoke().Cast<object>().ToList();

			output.Should().AllBeAssignableTo<IRecord>();
			output.Cast<IRecord>().Select(record => record.LineNumber).Should().Equal(3, 5, 8);
		}

		// Regression: Issue #1002
		[TestMethod]
		public void GivenIncludeAndExcludeFilters_WhenGettingRecords_ThenReturnsOneRecordPerMatch()
		{
			var cmdlet = new GetRecordsCmdlet
			{
				FilePath = new Daten().AsFilePath("Application.log"),
				Include = "database",
				Exclude = "Unable",
			};

			var output = cmdlet.Invoke().Cast<object>().ToList();

			output.Should().AllBeAssignableTo<IRecord>();
			output.Cast<IRecord>().Select(record => record.LineNumber).Should().Equal(2, 6);
		}
	}
}
