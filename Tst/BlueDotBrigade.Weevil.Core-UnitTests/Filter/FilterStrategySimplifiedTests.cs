using System.Collections.Concurrent;
using System.Collections.Immutable;
using BlueDotBrigade.Weevil.Data;
using BlueDotBrigade.Weevil.Filter;
using NSubstitute;

namespace BlueDotBrigade.Weevil.Core.UnitTests.Filter
{
	/// <summary>
	/// Simplified data-driven unit tests for FilterStrategy.CanKeep() method.
	/// Organized by filter type (Include/Exclude) for maximum clarity.
	/// </summary>
	[TestClass]
	public class FilterStrategySimplifiedTests
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

		#region Include Filter Tests

		/// <summary>
		/// Test CanKeep with Include filter covering all combinations of:
		/// - Record content: matches or doesn't match the filter
		/// - isPinned: true/false
		/// - isBookmarked: true/false  
		/// - showPinned: true/false
		/// - showBookmarks: true/false
		/// Expected result provided as last parameter.
		/// </summary>
		[TestMethod]
		// Include matches - always true
		[DataRow(true,  false, false, false, false, true,  DisplayName = "Match   | NotPinned | NotBookmarked | ShowPinnedOff | ShowBookmarksOff")]
		[DataRow(true,  false, false, true,  false, true,  DisplayName = "Match   | NotPinned | NotBookmarked | ShowPinnedOn  | ShowBookmarksOff")]
		[DataRow(true,  false, false, false, true,  true,  DisplayName = "Match   | NotPinned | NotBookmarked | ShowPinnedOff | ShowBookmarksOn ")]
		[DataRow(true,  false, false, true,  true,  true,  DisplayName = "Match   | NotPinned | NotBookmarked | ShowPinnedOn  | ShowBookmarksOn ")]
		[DataRow(true,  true,  false, false, false, true,  DisplayName = "Match   | Pinned    | NotBookmarked | ShowPinnedOff | ShowBookmarksOff")]
		[DataRow(true,  true,  false, true,  false, true,  DisplayName = "Match   | Pinned    | NotBookmarked | ShowPinnedOn  | ShowBookmarksOff")]
		[DataRow(true,  false, true,  false, false, true,  DisplayName = "Match   | NotPinned | Bookmarked    | ShowPinnedOff | ShowBookmarksOff")]
		[DataRow(true,  false, true,  false, true,  true,  DisplayName = "Match   | NotPinned | Bookmarked    | ShowPinnedOff | ShowBookmarksOn ")]
		// Include no match - false unless special record with option ON
		[DataRow(false, false, false, false, false, false, DisplayName = "NoMatch | NotPinned | NotBookmarked | ShowPinnedOff | ShowBookmarksOff")]
		[DataRow(false, false, false, true,  false, false, DisplayName = "NoMatch | NotPinned | NotBookmarked | ShowPinnedOn  | ShowBookmarksOff")]
		[DataRow(false, false, false, false, true,  false, DisplayName = "NoMatch | NotPinned | NotBookmarked | ShowPinnedOff | ShowBookmarksOn ")]
		[DataRow(false, true,  false, false, false, false, DisplayName = "NoMatch | Pinned    | NotBookmarked | ShowPinnedOff | ShowBookmarksOff")]
		[DataRow(false, true,  false, true,  false, true,  DisplayName = "NoMatch | Pinned    | NotBookmarked | ShowPinnedOn  | ShowBookmarksOff")]
		[DataRow(false, false, true,  false, false, false, DisplayName = "NoMatch | NotPinned | Bookmarked    | ShowPinnedOff | ShowBookmarksOff")]
		[DataRow(false, false, true,  false, true,  true,  DisplayName = "NoMatch | NotPinned | Bookmarked    | ShowPinnedOff | ShowBookmarksOn ")]
		public void IncludeFilter_VariousCombinations(bool contentMatches, bool isPinned, bool isBookmarked, bool showPinned, bool showBookmarks, bool expectedResult)
		{
			// Arrange
			var content = contentMatches ? SAMPLE_CONTENT_MATCH : SAMPLE_CONTENT_NO_MATCH;
			var record = CreateRecord(content, SAMPLE_LINE_NUMBER, isPinned);
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
			result.Should().Be(expectedResult);
		}

		#endregion

		#region Exclude Filter Tests

		/// <summary>
		/// Test CanKeep with Exclude filter covering all combinations of:
		/// - Record content: matches or doesn't match the filter
		/// - isPinned: true/false
		/// - isBookmarked: true/false
		/// - showPinned: true/false
		/// - showBookmarks: true/false
		/// Expected result provided as last parameter.
		/// </summary>
		[TestMethod]
		// Exclude matches - false unless special record with option ON
		[DataRow(true,  false, false, false, false, false, DisplayName = "Match   | NotPinned | NotBookmarked | ShowPinnedOff | ShowBookmarksOff")]
		[DataRow(true,  false, false, true,  false, false, DisplayName = "Match   | NotPinned | NotBookmarked | ShowPinnedOn  | ShowBookmarksOff")]
		[DataRow(true,  false, false, false, true,  false, DisplayName = "Match   | NotPinned | NotBookmarked | ShowPinnedOff | ShowBookmarksOn ")]
		[DataRow(true,  false, false, true,  true,  false, DisplayName = "Match   | NotPinned | NotBookmarked | ShowPinnedOn  | ShowBookmarksOn ")]
		[DataRow(true,  true,  false, false, false, false, DisplayName = "Match   | Pinned    | NotBookmarked | ShowPinnedOff | ShowBookmarksOff")]
		[DataRow(true,  true,  false, true,  false, true,  DisplayName = "Match   | Pinned    | NotBookmarked | ShowPinnedOn  | ShowBookmarksOff")]
		[DataRow(true,  false, true,  false, false, false, DisplayName = "Match   | NotPinned | Bookmarked    | ShowPinnedOff | ShowBookmarksOff")]
		[DataRow(true,  false, true,  false, true,  true,  DisplayName = "Match   | NotPinned | Bookmarked    | ShowPinnedOff | ShowBookmarksOn ")]
		// Exclude no match - true unless ShowPinned/ShowBookmarks ON without being special
		[DataRow(false, false, false, false, false, true,  DisplayName = "NoMatch | NotPinned | NotBookmarked | ShowPinnedOff | ShowBookmarksOff")]
		[DataRow(false, false, false, true,  false, false, DisplayName = "NoMatch | NotPinned | NotBookmarked | ShowPinnedOn  | ShowBookmarksOff")]
		[DataRow(false, false, false, false, true,  false, DisplayName = "NoMatch | NotPinned | NotBookmarked | ShowPinnedOff | ShowBookmarksOn ")]
		[DataRow(false, false, false, true,  true,  false, DisplayName = "NoMatch | NotPinned | NotBookmarked | ShowPinnedOn  | ShowBookmarksOn ")]
		[DataRow(false, true,  false, true,  false, true,  DisplayName = "NoMatch | Pinned    | NotBookmarked | ShowPinnedOn  | ShowBookmarksOff")]
		[DataRow(false, false, true,  false, true,  true,  DisplayName = "NoMatch | NotPinned | Bookmarked    | ShowPinnedOff | ShowBookmarksOn ")]
		public void ExcludeFilter_VariousCombinations(bool contentMatches, bool isPinned, bool isBookmarked, bool showPinned, bool showBookmarks, bool expectedResult)
		{
			// Arrange
			var content = contentMatches ? SAMPLE_CONTENT_MATCH : SAMPLE_CONTENT_NO_MATCH;
			var record = CreateRecord(content, SAMPLE_LINE_NUMBER, isPinned);
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
			result.Should().Be(expectedResult);
		}

		#endregion

		#region No Filters Test

		/// <summary>
		/// Test CanKeep with no filters - should always return true regardless of options.
		/// </summary>
		[TestMethod]
		[DataRow(false, false, false, false, DisplayName = "NotPinned | NotBookmarked | ShowPinnedOff | ShowBookmarksOff")]
		[DataRow(false, false, true,  false, DisplayName = "NotPinned | NotBookmarked | ShowPinnedOn  | ShowBookmarksOff")]
		[DataRow(false, false, false, true,  DisplayName = "NotPinned | NotBookmarked | ShowPinnedOff | ShowBookmarksOn ")]
		[DataRow(false, false, true,  true,  DisplayName = "NotPinned | NotBookmarked | ShowPinnedOn  | ShowBookmarksOn ")]
		[DataRow(true,  false, false, false, DisplayName = "Pinned    | NotBookmarked | ShowPinnedOff | ShowBookmarksOff")]
		[DataRow(true,  false, true,  false, DisplayName = "Pinned    | NotBookmarked | ShowPinnedOn  | ShowBookmarksOff")]
		[DataRow(true,  false, false, true,  DisplayName = "Pinned    | NotBookmarked | ShowPinnedOff | ShowBookmarksOn ")]
		[DataRow(false, true,  false, false, DisplayName = "NotPinned | Bookmarked    | ShowPinnedOff | ShowBookmarksOff")]
		[DataRow(false, true,  false, true,  DisplayName = "NotPinned | Bookmarked    | ShowPinnedOff | ShowBookmarksOn ")]
		[DataRow(false, true,  true,  false, DisplayName = "NotPinned | Bookmarked    | ShowPinnedOn  | ShowBookmarksOff")]
		[DataRow(false, true,  true,  true,  DisplayName = "NotPinned | Bookmarked    | ShowPinnedOn  | ShowBookmarksOn ")]
		[DataRow(true,  true,  true,  false, DisplayName = "Pinned    | Bookmarked    | ShowPinnedOn  | ShowBookmarksOff")]
		[DataRow(true,  true,  false, true,  DisplayName = "Pinned    | Bookmarked    | ShowPinnedOff | ShowBookmarksOn ")]
		[DataRow(true,  true,  true,  true,  DisplayName = "Pinned    | Bookmarked    | ShowPinnedOn  | ShowBookmarksOn ")]
		public void NoFilters_AlwaysReturnsTrue(bool isPinned, bool isBookmarked, bool showPinned, bool showBookmarks)
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
			result.Should().BeTrue("with no filters, all records should be visible");
		}

		#endregion

		#region Both Filters Test

		/// <summary>
		/// Test CanKeep when both Include and Exclude filters are present.
		/// Include matches but exclude takes priority unless special record overrides.
		/// </summary>
		[TestMethod]
		[DataRow(false, false, false, false, false, DisplayName = "NotPinned | NotBookmarked | ShowPinnedOff | ShowBookmarksOff")]
		[DataRow(true,  false, true,  false, true,  DisplayName = "Pinned    | NotBookmarked | ShowPinnedOn  | ShowBookmarksOff")]
		[DataRow(false, true,  false, true,  true,  DisplayName = "NotPinned | Bookmarked    | ShowPinnedOff | ShowBookmarksOn ")]
		public void BothFilters_ExcludeTakesPriority(bool isPinned, bool isBookmarked, bool showPinned, bool showBookmarks, bool expectedResult)
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
			result.Should().Be(expectedResult, "exclude takes priority unless special record overrides");
		}

		#endregion
	}
}
