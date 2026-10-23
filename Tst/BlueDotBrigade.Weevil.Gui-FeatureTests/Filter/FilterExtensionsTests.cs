namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.Filter;

	[TestClass]
	public class FilterExtensionsTests
	{
		private string _logPath = string.Empty;

		[TestInitialize]
		public void Setup()
		{
			_logPath = Path.Combine(Path.GetTempPath(), $"FilterExtensionsTests.{Guid.NewGuid():N}.log");
			File.WriteAllLines(_logPath, Enumerable.Range(1, 10).Select(n => $"Record {n}"));
		}

		[TestCleanup]
		public void Cleanup()
		{
			File.Delete(_logPath);
			File.Delete(_logPath + ".xml");
		}

		[TestMethod]
		public async Task GivenSelectedRecords_WhenTheFilterExpressionIsInvalid_ThenTheSelectionIsKept()
		{
			// Regression: #1064. The filter cleared the selection first, and only an aborted filter restored it.
			IEngine engine = Engine.UsingPath(_logPath).Open();
			engine.Selector.SelectOnly(2, 4);

			Func<Task> filter = () => engine.FilterAsync(FilterType.RegularExpression, new FilterCriteria("Record ("), CancellationToken.None);

			await filter.Should().ThrowAsync<Exception>();
			engine.Selector.Selected.Keys.Should().Equal(2, 3, 4);
		}

		[TestMethod]
		public async Task GivenSelectedRecords_WhenFiltered_ThenTheRecordsStillShownStaySelected()
		{
			IEngine engine = Engine.UsingPath(_logPath).Open();
			engine.Selector.SelectOnly(2, 4);

			var wasFiltered = await engine.FilterAsync(FilterType.PlainText, new FilterCriteria("Record 3"), CancellationToken.None);

			wasFiltered.Should().BeTrue();
			engine.Selector.Selected.Keys.Should().Equal(3);
		}
	}
}
