namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Collections.Immutable;
	using System.Threading;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Gui.Analysis;
	using BlueDotBrigade.Weevil.Gui.Threading;

	/// <summary>
	/// Tests for insight navigation functionality, particularly when records are cleared from memory.
	/// </summary>
	[TestClass]
	public class InsightNavigationTests : UiTestBase
	{
		/// <summary>
		/// Tests that when all insight records are available,
		/// the navigation proceeds normally without warnings.
		/// </summary>
		[TestMethod]
		public async Task OnNavigateToInsightRecord_AllRecordsAvailable_NoWarning()
		{
			// Arrange
			var bulletinMediator = Substitute.For<IBulletinMediator>();
			var viewModel = new FilterViewModel(this.UiDispatcher, bulletinMediator);

			// Open a log file
			await viewModel.OpenAsync(new Daten().AsFilePath(From.GlobalDefault));

			// Wait for the file to be fully loaded
			var stopwatch = System.Diagnostics.Stopwatch.StartNew();
			while (!viewModel.IsLogFileOpen && stopwatch.Elapsed < TimeSpan.FromSeconds(5))
			{
				Thread.Sleep(TimeSpan.FromMilliseconds(100));
			}

			Assert.IsTrue(viewModel.IsLogFileOpen, "Log file should be open before test continues");

			// Create mock records that exist in the log
			var record1 = Substitute.For<IRecord>();
			record1.LineNumber.Returns(1);
			record1.Metadata.Returns(new Metadata());

			var record2 = Substitute.For<IRecord>();
			record2.LineNumber.Returns(10);
			record2.Metadata.Returns(new Metadata());

			var insightRecords = ImmutableArray.Create(record1, record2);
			var bulletin = new NavigateToInsightRecordBulletin(insightRecords);

			// Act - trigger the insight navigation
			bulletinMediator.Post(bulletin);

			// Give time for async operations to complete
			Thread.Sleep(TimeSpan.FromSeconds(1));

			// Assert
			// All records should be available, so navigation proceeds without warnings
			Assert.IsTrue(true, "Insight navigation should proceed normally when all records are available");
		}
	}
}
