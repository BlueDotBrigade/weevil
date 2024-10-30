﻿namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Diagnostics;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Windows;
	using BlueDotBrigade.DatenLokator.TestsTools;
	using BlueDotBrigade.Weevil.Gui.Threading;
	using BlueDotBrigade.Weevil.TestingTools;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using NSubstitute;

	// Adding this attribute prevents the exception: InvalidOperationException The calling thread must be STA, because many UI components require this.
	// Mention this in: https://github.com/BlueDotBrigade/weevil/issues/191
	//[StaTestClass]
	public class FilterResultsViewModelTests : UiTestBase
	{
		[TestMethod]
		public async Task OpenAsync()
		{
			var bulletinMediator = Substitute.For<IBulletinMediator>();

			var viewModel = new FilterViewModel(
				this.UiDispatcher,
				bulletinMediator);

			await viewModel.OpenAsync(new Daten().AsFilePath(From.GlobalDefault));

			viewModel.IsManualFilter = true;
			viewModel.FilterManually(new object[]
			{
				"@IsPinned",
				string.Empty,
			});

			var stopwatch = Stopwatch.StartNew();
			do
			{
				Thread.Sleep(TimeSpan.FromMilliseconds(100));
			} while (viewModel.IsFilterInProgress && stopwatch.Elapsed < TimeSpan.FromSeconds(5));

			Assert.AreEqual(512, viewModel.VisibleItems.Count);
		}
	}
}