namespace BlueDotBrigade.Weevil.Filter
{
	using System.Diagnostics;

	/// <summary>
	/// Checks that a filter finds the same records in each encoding, without becoming much slower.
	/// </summary>
	[TestClass]
	public class FilterManagerTests
	{
		// Generous, so a slow build machine does not fail the test: each filter took about 0.6 s on 4 cores in a Debug build (2026-10-30).
		// #1089 compares with Weevil 2.12.1, whose times on the reference machine are not measured yet.
		private static readonly TimeSpan MaximumDuration = TimeSpan.FromSeconds(10);

		public TestContext TestContext { get; set; } = null!;

		[TestMethod]
		[WorkItem(1089)]
		[DoNotParallelize]
		[DataRow(DroidLog.Utf8, FilterType.PlainText, "Blaster", 23)]
		[DataRow(DroidLog.Utf16LittleEndian, FilterType.PlainText, "Blaster", 23)]
		[DataRow(DroidLog.Utf8, FilterType.RegularExpression, @"\[Security\].*detected", 34)]
		[DataRow(DroidLog.Utf16LittleEndian, FilterType.RegularExpression, @"\[Security\].*detected", 34)]
		public void GivenADroidLog_WhenFiltered_ThenTheMatchingRecordsAreFoundQuickly(string logName, FilterType filterType, string expression, int matchingRecords)
		{
			IEngine engine = Engine.UsingPath(DroidLog.GetPath(logName)).Open();

			var stopwatch = Stopwatch.StartNew();
			engine.Filter.Apply(filterType, new FilterCriteria(expression));
			stopwatch.Stop();

			TestContext.WriteLine($"Filtered {engine.Count:N0} records in {stopwatch.Elapsed.TotalSeconds:N2} s.");
			engine.Filter.Results.Should().HaveCount(matchingRecords);
			stopwatch.Elapsed.Should().BeLessThan(MaximumDuration);
		}
	}
}
