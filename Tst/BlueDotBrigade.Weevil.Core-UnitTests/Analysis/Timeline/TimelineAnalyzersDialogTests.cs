namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.IO;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using NSubstitute;

	/// <summary>
	/// Tests for custom regex dialog functionality in timeline analyzers.
	/// </summary>
	[TestClass]
	public class TimelineAnalyzersDialogTests
	{
		private IUserDialog GetUserDialogWithAnalysisDialogSupport(bool shouldCancel, string regexToReturn)
		{
			var userDialog = Substitute.For<IUserDialog>();

			// For rising/falling edge analyzers, mock GetAnalysisOrder prompt
			userDialog
				.ShowUserPrompt(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
				.Returns("Ascending");

			// Mock TryGetExpressions
			userDialog
				.TryGetExpressions(Arg.Any<string>(), Arg.Any<string>(), out Arg.Any<string>())
				.Returns(x =>
				{
					if (shouldCancel)
					{
						x[2] = null;
						return false;
					}
					x[2] = regexToReturn;
					return true;
				});

			return userDialog;
		}

		private FilterStrategy GetFilterStrategy()
		{
			// Return a minimal FilterStrategy that doesn't throw
			// For these tests, we mainly care about the dialog behavior
			return FilterStrategy.KeepAllRecords;
		}

		#region DataTransitionAnalyzer Tests

		[TestMethod]
		public void DataTransitionAnalyzer_UserCancelsDialog_ReturnsZeroResults()
		{
			// Arrange
			var analyzer = new DataTransitionAnalyzer(GetFilterStrategy());
			var userDialog = GetUserDialogWithAnalysisDialogSupport(shouldCancel: true, regexToReturn: null);
			var records = ImmutableArray<IRecord>.Empty;

			// Act
			var results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);

			// Assert
			Assert.AreEqual(0, results.FlaggedRecords);
		}

		[TestMethod]
		public void DataTransitionAnalyzer_BlankRegex_ReturnsZeroResults()
		{
			// Arrange
			var analyzer = new DataTransitionAnalyzer(GetFilterStrategy());
			var userDialog = GetUserDialogWithAnalysisDialogSupport(shouldCancel: false, regexToReturn: "   ");
			var records = ImmutableArray<IRecord>.Empty;

			// Act
			var results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);

			// Assert
			Assert.AreEqual(0, results.FlaggedRecords);
		}

		[TestMethod]
		public void DataTransitionAnalyzer_EmptyStringRegex_ReturnsZeroResults()
		{
			// Arrange
			var analyzer = new DataTransitionAnalyzer(GetFilterStrategy());
			var userDialog = GetUserDialogWithAnalysisDialogSupport(shouldCancel: false, regexToReturn: string.Empty);
			var records = ImmutableArray<IRecord>.Empty;

			// Act
			var results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);

			// Assert
			Assert.AreEqual(0, results.FlaggedRecords);
		}

		#endregion

		#region StableValueAnalyzer Tests

		[TestMethod]
		public void StableValueAnalyzer_UserCancelsDialog_ReturnsZeroResults()
		{
			// Arrange
			var analyzer = new StableValueAnalyzer(GetFilterStrategy());
			var userDialog = GetUserDialogWithAnalysisDialogSupport(shouldCancel: true, regexToReturn: null);
			var records = ImmutableArray<IRecord>.Empty;

			// Act
			var results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);

			// Assert
			Assert.AreEqual(0, results.FlaggedRecords);
		}

		[TestMethod]
		public void StableValueAnalyzer_BlankRegex_ReturnsZeroResults()
		{
			// Arrange
			var analyzer = new StableValueAnalyzer(GetFilterStrategy());
			var userDialog = GetUserDialogWithAnalysisDialogSupport(shouldCancel: false, regexToReturn: "   ");
			var records = ImmutableArray<IRecord>.Empty;

			// Act
			var results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);

			// Assert
			Assert.AreEqual(0, results.FlaggedRecords);
		}

		#endregion

		#region DetectRisingEdgeAnalyzer Tests

		[TestMethod]
		public void DetectRisingEdgeAnalyzer_UserCancelsDialog_ReturnsZeroResults()
		{
			// Arrange
			var analyzer = new DetectRisingEdgeAnalyzer(GetFilterStrategy());
			var userDialog = GetUserDialogWithAnalysisDialogSupport(shouldCancel: true, regexToReturn: null);
			var records = ImmutableArray<IRecord>.Empty;

			// Act
			var results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);

			// Assert
			Assert.AreEqual(0, results.FlaggedRecords);
		}

		[TestMethod]
		public void DetectRisingEdgeAnalyzer_BlankRegex_ReturnsZeroResults()
		{
			// Arrange
			var analyzer = new DetectRisingEdgeAnalyzer(GetFilterStrategy());
			var userDialog = GetUserDialogWithAnalysisDialogSupport(shouldCancel: false, regexToReturn: "");
			var records = ImmutableArray<IRecord>.Empty;

			// Act
			var results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);

			// Assert
			Assert.AreEqual(0, results.FlaggedRecords);
		}

		#endregion

		#region DetectFallingEdgeAnalyzer Tests

		[TestMethod]
		public void DetectFallingEdgeAnalyzer_UserCancelsDialog_ReturnsZeroResults()
		{
			// Arrange
			var analyzer = new DetectFallingEdgeAnalyzer(GetFilterStrategy());
			var userDialog = GetUserDialogWithAnalysisDialogSupport(shouldCancel: true, regexToReturn: null);
			var records = ImmutableArray<IRecord>.Empty;

			// Act
			var results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);

			// Assert
			Assert.AreEqual(0, results.FlaggedRecords);
		}

		[TestMethod]
		public void DetectFallingEdgeAnalyzer_BlankRegex_ReturnsZeroResults()
		{
			// Arrange
			var analyzer = new DetectFallingEdgeAnalyzer(GetFilterStrategy());
			var userDialog = GetUserDialogWithAnalysisDialogSupport(shouldCancel: false, regexToReturn: "  ");
			var records = ImmutableArray<IRecord>.Empty;

			// Act
			var results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);

			// Assert
			Assert.AreEqual(0, results.FlaggedRecords);
		}

		#endregion

		#region DetectRepeatingRecordsAnalyzer Tests

		[TestMethod]
		public void DetectRepeatingRecordsAnalyzer_UserCancelsDialog_ReturnsZeroResults()
		{
			// Arrange
			var analyzer = new DetectRepeatingRecordsAnalyzer(GetFilterStrategy());
			var userDialog = GetUserDialogWithAnalysisDialogSupport(shouldCancel: true, regexToReturn: null);
			var records = ImmutableArray<IRecord>.Empty;

			// Act
			var results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);

			// Assert
			Assert.AreEqual(0, results.FlaggedRecords);
		}

		[TestMethod]
		public void DetectRepeatingRecordsAnalyzer_BlankRegex_ReturnsZeroResults()
		{
			// Arrange
			var analyzer = new DetectRepeatingRecordsAnalyzer(GetFilterStrategy());
			var userDialog = GetUserDialogWithAnalysisDialogSupport(shouldCancel: false, regexToReturn: "   ");
			var records = ImmutableArray<IRecord>.Empty;

			// Act
			var results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);

			// Assert
			Assert.AreEqual(0, results.FlaggedRecords);
		}

		[TestMethod]
		public void DetectRepeatingRecordsAnalyzer_EmptyStringRegex_ReturnsZeroResults()
		{
			// Arrange
			var analyzer = new DetectRepeatingRecordsAnalyzer(GetFilterStrategy());
			var userDialog = GetUserDialogWithAnalysisDialogSupport(shouldCancel: false, regexToReturn: string.Empty);
			var records = ImmutableArray<IRecord>.Empty;

			// Act
			var results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);

			// Assert
			Assert.AreEqual(0, results.FlaggedRecords);
		}

		#endregion

		#region StatisticalAnalyzer Tests

		[TestMethod]
		public void StatisticalAnalyzer_UserCancelsDialog_ReturnsZeroResults()
		{
			// Arrange
			var analyzer = new StatisticalAnalyzer(GetFilterStrategy());
			var userDialog = GetUserDialogWithAnalysisDialogSupport(shouldCancel: true, regexToReturn: null);
			var records = ImmutableArray<IRecord>.Empty;

			// Act
			var results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);

			// Assert
			Assert.AreEqual(0, results.FlaggedRecords);
		}

		[TestMethod]
		public void StatisticalAnalyzer_BlankRegex_ReturnsZeroResults()
		{
			// Arrange
			var analyzer = new StatisticalAnalyzer(GetFilterStrategy());
			var userDialog = GetUserDialogWithAnalysisDialogSupport(shouldCancel: false, regexToReturn: "   ");
			var records = ImmutableArray<IRecord>.Empty;

			// Act
			var results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);

			// Assert
			Assert.AreEqual(0, results.FlaggedRecords);
		}

		[TestMethod]
		public void StatisticalAnalyzer_EmptyStringRegex_ReturnsZeroResults()
		{
			// Arrange
			var analyzer = new StatisticalAnalyzer(GetFilterStrategy());
			var userDialog = GetUserDialogWithAnalysisDialogSupport(shouldCancel: false, regexToReturn: string.Empty);
			var records = ImmutableArray<IRecord>.Empty;

			// Act
			var results = analyzer.Analyze(records, string.Empty, userDialog, canUpdateMetadata: true);

			// Assert
			Assert.AreEqual(0, results.FlaggedRecords);
		}

		#endregion
	}
}
