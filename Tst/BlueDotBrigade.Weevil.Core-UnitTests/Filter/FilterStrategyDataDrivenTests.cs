using System.Collections.Concurrent;
using System.Collections.Immutable;
using BlueDotBrigade.Weevil.Data;
using BlueDotBrigade.Weevil.Filter;
using NSubstitute;

namespace BlueDotBrigade.Weevil.Core.UnitTests.Filter
{
	/// <summary>
	/// Unit tests for FilterStrategy.CanKeep() method using DataRow for improved readability.
	/// Tests verify correct behavior with all combinations of include/exclude filters,
	/// pinned/bookmarked records, and configuration options.
	/// </summary>
	[TestClass]
	public class FilterStrategyDataDrivenTests
	{
		// Sample data used consistently across all tests
		private const string SAMPLE_CONTENT_MATCH = "ERROR: System failure";
		private const string SAMPLE_CONTENT_NO_MATCH = "INFO: System started";
		private const int SAMPLE_LINE_NUMBER = 42;

		#region Test Infrastructure

		private static IRecord CreateRecord(string content, int lineNumber, bool isPinned)
		{
			var record = Substitute.For<IRecord>();
			record.Content.Returns(content);
			record.LineNumber.Returns(lineNumber);
			
			var metadata = new Metadata();
			if (isPinned)
			{
				metadata.IsPinned = true;
			}
			record.Metadata.Returns(metadata);
			
			return record;
		}

		private static IBookmarkManager CreateBookmarkManager(bool hasBookmark, int lineNumber)
		{
			var bookmarkManager = Substitute.For<IBookmarkManager>();
			string bookmarkName;
			bookmarkManager.TryGetBookmarkName(lineNumber, out bookmarkName)
				.Returns(x =>
				{
					if (hasBookmark)
					{
						x[1] = "TestBookmark";
						return true;
					}
					x[1] = null;
					return false;
				});
			return bookmarkManager;
		}

		private static FilterStrategy CreateFilterStrategy(
			string includeFilter,
			string excludeFilter,
			bool showPinned,
			bool showBookmarks,
			IBookmarkManager bookmarkManager)
		{
			var coreExtension = Substitute.For<ICoreExtension>();
			var context = new ContextDictionary();
			var filterAliasExpander = Substitute.For<IFilterAliasExpander>();
			filterAliasExpander.Expand(Arg.Any<string>()).Returns(x => x.Arg<string>());
			
			var configuration = new ConcurrentDictionary<string, object>();
			configuration["IncludePinned"] = showPinned;
			configuration["IncludeBookmarks"] = showBookmarks;
			
			var filterCriteria = new FilterCriteria(includeFilter, excludeFilter, configuration);
			var regionManager = Substitute.For<IRegionManager>();
			
			return new FilterStrategy(
				coreExtension,
				context,
				filterAliasExpander,
				FilterType.PlainText,
				filterCriteria,
				regionManager,
				bookmarkManager);
		}

		#endregion

		#region No Filters Scenarios (DataRow Format)

		/// <summary>
		/// Test CanKeep with no filters - should always return true regardless of show options.
		/// </summary>
		/// <param name="isPinned">Whether the record is pinned</param>
		/// <param name="isBookmarked">Whether the record is bookmarked</param>
		/// <param name="showPinned">ShowPinned configuration option</param>
		/// <param name="showBookmarks">ShowBookmarks configuration option</param>
		[TestMethod]
		[DataRow(false, false, false, false, DisplayName = "NotPinned | NotBookmarked | ShowPinnedOff  | ShowBookmarksOff")]
		[DataRow(false, false, true,  false, DisplayName = "NotPinned | NotBookmarked | ShowPinnedOn   | ShowBookmarksOff")]
		[DataRow(false, false, false, true,  DisplayName = "NotPinned | NotBookmarked | ShowPinnedOff  | ShowBookmarksOn ")]
		[DataRow(false, false, true,  true,  DisplayName = "NotPinned | NotBookmarked | ShowPinnedOn   | ShowBookmarksOn ")]
		[DataRow(true,  false, false, false, DisplayName = "Pinned    | NotBookmarked | ShowPinnedOff  | ShowBookmarksOff")]
		[DataRow(true,  false, true,  false, DisplayName = "Pinned    | NotBookmarked | ShowPinnedOn   | ShowBookmarksOff")]
		[DataRow(true,  false, false, true,  DisplayName = "Pinned    | NotBookmarked | ShowPinnedOff  | ShowBookmarksOn ")]
		[DataRow(false, true,  false, false, DisplayName = "NotPinned | Bookmarked    | ShowPinnedOff  | ShowBookmarksOff")]
		[DataRow(false, true,  false, true,  DisplayName = "NotPinned | Bookmarked    | ShowPinnedOff  | ShowBookmarksOn ")]
		[DataRow(false, true,  true,  false, DisplayName = "NotPinned | Bookmarked    | ShowPinnedOn   | ShowBookmarksOff")]
		[DataRow(false, true,  true,  true,  DisplayName = "NotPinned | Bookmarked    | ShowPinnedOn   | ShowBookmarksOn ")]
		[DataRow(true,  true,  true,  false, DisplayName = "Pinned    | Bookmarked    | ShowPinnedOn   | ShowBookmarksOff")]
		[DataRow(true,  true,  false, true,  DisplayName = "Pinned    | Bookmarked    | ShowPinnedOff  | ShowBookmarksOn ")]
		[DataRow(true,  true,  true,  true,  DisplayName = "Pinned    | Bookmarked    | ShowPinnedOn   | ShowBookmarksOn ")]
		public void CanKeep_NoFilters_AlwaysReturnsTrue(bool isPinned, bool isBookmarked, bool showPinned, bool showBookmarks)
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned);
			var bookmarkManager = CreateBookmarkManager(isBookmarked, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: string.Empty,
				showPinned: showPinned,
				showBookmarks: showBookmarks,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue("with no filters, all records should be visible regardless of show options");
		}

		#endregion

		#region Include Filter Scenarios (DataRow Format)

		/// <summary>
		/// Test CanKeep with include filter that matches the record content.
		/// Expected: True when record matches filter OR when special record with show option ON.
		/// </summary>
		[TestMethod]
		[DataRow(false, false, false, false, DisplayName = "NotPinned | NotBookmarked | ShowPinnedOff  | ShowBookmarksOff")]
		[DataRow(false, false, true,  false, DisplayName = "NotPinned | NotBookmarked | ShowPinnedOn   | ShowBookmarksOff")]
		[DataRow(false, false, false, true,  DisplayName = "NotPinned | NotBookmarked | ShowPinnedOff  | ShowBookmarksOn ")]
		[DataRow(false, false, true,  true,  DisplayName = "NotPinned | NotBookmarked | ShowPinnedOn   | ShowBookmarksOn ")]
		[DataRow(true,  false, false, false, DisplayName = "Pinned    | NotBookmarked | ShowPinnedOff  | ShowBookmarksOff")]
		[DataRow(true,  false, true,  false, DisplayName = "Pinned    | NotBookmarked | ShowPinnedOn   | ShowBookmarksOff")]
		[DataRow(false, true,  false, false, DisplayName = "NotPinned | Bookmarked    | ShowPinnedOff  | ShowBookmarksOff")]
		[DataRow(false, true,  false, true,  DisplayName = "NotPinned | Bookmarked    | ShowPinnedOff  | ShowBookmarksOn ")]
		public void CanKeep_IncludeMatches_ReturnsTrue(bool isPinned, bool isBookmarked, bool showPinned, bool showBookmarks)
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_MATCH, SAMPLE_LINE_NUMBER, isPinned);
			var bookmarkManager = CreateBookmarkManager(isBookmarked, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: "ERROR",
				excludeFilter: string.Empty,
				showPinned: showPinned,
				showBookmarks: showBookmarks,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue("record matches include filter");
		}

		/// <summary>
		/// Test CanKeep with include filter that does NOT match the record content.
		/// Expected: False unless record is pinned/bookmarked with corresponding show option ON.
		/// </summary>
		[TestMethod]
		[DataRow(false, false, false, false, false, DisplayName = "NotPinned | NotBookmarked | ShowPinnedOff  | ShowBookmarksOff")]
		[DataRow(false, false, true,  false, false, DisplayName = "NotPinned | NotBookmarked | ShowPinnedOn   | ShowBookmarksOff")]
		[DataRow(false, false, false, true,  false, DisplayName = "NotPinned | NotBookmarked | ShowPinnedOff  | ShowBookmarksOn ")]
		[DataRow(true,  false, false, false, false, DisplayName = "Pinned    | NotBookmarked | ShowPinnedOff  | ShowBookmarksOff")]
		[DataRow(true,  false, true,  false, true,  DisplayName = "Pinned    | NotBookmarked | ShowPinnedOn   | ShowBookmarksOff")]
		[DataRow(false, true,  false, false, false, DisplayName = "NotPinned | Bookmarked    | ShowPinnedOff  | ShowBookmarksOff")]
		[DataRow(false, true,  false, true,  true,  DisplayName = "NotPinned | Bookmarked    | ShowPinnedOff  | ShowBookmarksOn ")]
		public void CanKeep_IncludeNoMatch_ReturnsExpected(bool isPinned, bool isBookmarked, bool showPinned, bool showBookmarks, bool expectedResult)
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned);
			var bookmarkManager = CreateBookmarkManager(isBookmarked, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: "ERROR",
				excludeFilter: string.Empty,
				showPinned: showPinned,
				showBookmarks: showBookmarks,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().Be(expectedResult, 
				$"record does not match include filter - visible only if pinned/bookmarked with show option ON");
		}

		#endregion

		#region Exclude Filter Scenarios (DataRow Format)

		/// <summary>
		/// Test CanKeep with exclude filter that matches the record content.
		/// Expected: False unless record is pinned/bookmarked with corresponding show option ON.
		/// </summary>
		[TestMethod]
		[DataRow(false, false, false, false, false, DisplayName = "NotPinned | NotBookmarked | ShowPinnedOff  | ShowBookmarksOff")]
		[DataRow(false, false, true,  false, false, DisplayName = "NotPinned | NotBookmarked | ShowPinnedOn   | ShowBookmarksOff")]
		[DataRow(false, false, false, true,  false, DisplayName = "NotPinned | NotBookmarked | ShowPinnedOff  | ShowBookmarksOn ")]
		[DataRow(false, false, true,  true,  false, DisplayName = "NotPinned | NotBookmarked | ShowPinnedOn   | ShowBookmarksOn ")]
		[DataRow(true,  false, false, false, false, DisplayName = "Pinned    | NotBookmarked | ShowPinnedOff  | ShowBookmarksOff")]
		[DataRow(true,  false, true,  false, true,  DisplayName = "Pinned    | NotBookmarked | ShowPinnedOn   | ShowBookmarksOff")]
		[DataRow(false, true,  false, false, false, DisplayName = "NotPinned | Bookmarked    | ShowPinnedOff  | ShowBookmarksOff")]
		[DataRow(false, true,  false, true,  true,  DisplayName = "NotPinned | Bookmarked    | ShowPinnedOff  | ShowBookmarksOn ")]
		public void CanKeep_ExcludeMatches_ReturnsExpected(bool isPinned, bool isBookmarked, bool showPinned, bool showBookmarks, bool expectedResult)
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_MATCH, SAMPLE_LINE_NUMBER, isPinned);
			var bookmarkManager = CreateBookmarkManager(isBookmarked, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: "ERROR",
				showPinned: showPinned,
				showBookmarks: showBookmarks,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().Be(expectedResult, 
				$"record matches exclude filter - hidden unless pinned/bookmarked with show option ON");
		}

		/// <summary>
		/// Test CanKeep with exclude filter that does NOT match the record content.
		/// Expected: True when ShowPinned/ShowBookmarks OFF, or when record is special.
		///           False when ShowPinned/ShowBookmarks ON but record is not special (only show special records).
		/// </summary>
		[TestMethod]
		[DataRow(false, false, false, false, true,  DisplayName = "NotPinned | NotBookmarked | ShowPinnedOff  | ShowBookmarksOff")]
		[DataRow(false, false, true,  false, false, DisplayName = "NotPinned | NotBookmarked | ShowPinnedOn   | ShowBookmarksOff")]
		[DataRow(false, false, false, true,  false, DisplayName = "NotPinned | NotBookmarked | ShowPinnedOff  | ShowBookmarksOn ")]
		[DataRow(false, false, true,  true,  false, DisplayName = "NotPinned | NotBookmarked | ShowPinnedOn   | ShowBookmarksOn ")]
		[DataRow(true,  false, true,  false, true,  DisplayName = "Pinned    | NotBookmarked | ShowPinnedOn   | ShowBookmarksOff")]
		[DataRow(false, true,  false, true,  true,  DisplayName = "NotPinned | Bookmarked    | ShowPinnedOff  | ShowBookmarksOn ")]
		public void CanKeep_ExcludeNoMatch_ReturnsExpected(bool isPinned, bool isBookmarked, bool showPinned, bool showBookmarks, bool expectedResult)
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned);
			var bookmarkManager = CreateBookmarkManager(isBookmarked, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: "ERROR",
				showPinned: showPinned,
				showBookmarks: showBookmarks,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().Be(expectedResult,
				$"record does not match exclude - visible unless ShowPinned/ShowBookmarks ON without being special");
		}

		#endregion

		#region Include AND Exclude Filter Scenarios (DataRow Format)

		/// <summary>
		/// Test CanKeep when both include and exclude filters match the record.
		/// Expected: False (exclude takes priority) unless pinned/bookmarked with show option ON.
		/// </summary>
		[TestMethod]
		[DataRow(false, false, false, false, false, DisplayName = "NotPinned | NotBookmarked | ShowPinnedOff  | ShowBookmarksOff")]
		[DataRow(true,  false, true,  false, true,  DisplayName = "Pinned    | NotBookmarked | ShowPinnedOn   | ShowBookmarksOff")]
		[DataRow(false, true,  false, true,  true,  DisplayName = "NotPinned | Bookmarked    | ShowPinnedOff  | ShowBookmarksOn ")]
		public void CanKeep_BothFiltersMatch_ReturnsExpected(bool isPinned, bool isBookmarked, bool showPinned, bool showBookmarks, bool expectedResult)
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_MATCH, SAMPLE_LINE_NUMBER, isPinned);
			var bookmarkManager = CreateBookmarkManager(isBookmarked, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: "ERROR",
				excludeFilter: "ERROR",
				showPinned: showPinned,
				showBookmarks: showBookmarks,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().Be(expectedResult,
				$"exclude takes priority - hidden unless pinned/bookmarked with show option ON");
		}

		/// <summary>
		/// Test CanKeep when include matches but exclude does not.
		/// Expected: True (record passes both filters).
		/// </summary>
		[TestMethod]
		[DataRow(false, false, false, false, DisplayName = "NotPinned | NotBookmarked | ShowPinnedOff  | ShowBookmarksOff")]
		public void CanKeep_IncludeMatchesExcludeNoMatch_ReturnsTrue(bool isPinned, bool isBookmarked, bool showPinned, bool showBookmarks)
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_MATCH, SAMPLE_LINE_NUMBER, isPinned);
			var bookmarkManager = CreateBookmarkManager(isBookmarked, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: "ERROR",
				excludeFilter: "DEBUG",
				showPinned: showPinned,
				showBookmarks: showBookmarks,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue("record matches include and does not match exclude");
		}

		#endregion
	}
}
