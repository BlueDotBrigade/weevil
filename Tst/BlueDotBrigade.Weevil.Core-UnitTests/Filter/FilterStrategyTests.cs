using System.Collections.Concurrent;
using System.Collections.Immutable;
using BlueDotBrigade.Weevil.Data;
using BlueDotBrigade.Weevil.Filter;
using NSubstitute;

namespace BlueDotBrigade.Weevil.Core.UnitTests.Filter
{
	/// <summary>
	/// Unit tests for FilterStrategy.CanKeep() method to verify correct behavior
	/// with all combinations of include/exclude filters, pinned/bookmarked records,
	/// and configuration options.
	/// </summary>
	[TestClass]
	public class FilterStrategyTests
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

		#region No Filters - No Special Records

		[TestMethod]
		public void CanKeep_NoFilters_NotPinned_NotBookmarked_ShowPinnedOff_ShowBookmarksOff_ReturnsTrue()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: string.Empty,
				showPinned: false,
				showBookmarks: false,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue("no filters means all records should be visible");
		}

		[TestMethod]
		public void CanKeep_NoFilters_NotPinned_NotBookmarked_ShowPinnedOn_ShowBookmarksOff_ReturnsTrue()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: string.Empty,
				showPinned: true,
				showBookmarks: false,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue("with no filters, all records should be visible regardless of ShowPinned setting");
		}

		[TestMethod]
		public void CanKeep_NoFilters_NotPinned_NotBookmarked_ShowPinnedOff_ShowBookmarksOn_ReturnsTrue()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: string.Empty,
				showPinned: false,
				showBookmarks: true,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue("with no filters, all records should be visible regardless of ShowBookmarks setting");
		}

		[TestMethod]
		public void CanKeep_NoFilters_NotPinned_NotBookmarked_ShowPinnedOn_ShowBookmarksOn_ReturnsTrue()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: string.Empty,
				showPinned: true,
				showBookmarks: true,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue("with no filters, all records should be visible regardless of show options");
		}

		#endregion

		#region No Filters - Pinned Record

		[TestMethod]
		public void CanKeep_NoFilters_Pinned_NotBookmarked_ShowPinnedOff_ShowBookmarksOff_ReturnsTrue()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned: true);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: string.Empty,
				showPinned: false,
				showBookmarks: false,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue("no filters means all records should be visible");
		}

		[TestMethod]
		public void CanKeep_NoFilters_Pinned_NotBookmarked_ShowPinnedOn_ShowBookmarksOff_ReturnsTrue()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned: true);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: string.Empty,
				showPinned: true,
				showBookmarks: false,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue("pinned record should be visible when ShowPinned is ON");
		}

		[TestMethod]
		public void CanKeep_NoFilters_Pinned_NotBookmarked_ShowPinnedOff_ShowBookmarksOn_ReturnsTrue()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned: true);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: string.Empty,
				showPinned: false,
				showBookmarks: true,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue("with no filters, all records should be visible regardless of show options");
		}

		#endregion

		#region No Filters - Bookmarked Record

		[TestMethod]
		public void CanKeep_NoFilters_NotPinned_Bookmarked_ShowPinnedOff_ShowBookmarksOff_ReturnsTrue()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: true, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: string.Empty,
				showPinned: false,
				showBookmarks: false,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue("no filters means all records should be visible");
		}

		[TestMethod]
		public void CanKeep_NoFilters_NotPinned_Bookmarked_ShowPinnedOff_ShowBookmarksOn_ReturnsTrue()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: true, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: string.Empty,
				showPinned: false,
				showBookmarks: true,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue("bookmarked record should be visible when ShowBookmarks is ON");
		}

		[TestMethod]
		public void CanKeep_NoFilters_NotPinned_Bookmarked_ShowPinnedOn_ShowBookmarksOff_ReturnsTrue()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: true, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: string.Empty,
				showPinned: true,
				showBookmarks: false,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue("with no filters, all records should be visible regardless of show options");
		}

		[TestMethod]
		public void CanKeep_NoFilters_NotPinned_Bookmarked_ShowPinnedOn_ShowBookmarksOn_ReturnsTrue()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: true, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: string.Empty,
				showPinned: true,
				showBookmarks: true,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue("bookmarked record should be visible when ShowBookmarks is ON");
		}

		[TestMethod]
		public void CanKeep_NoFilters_Pinned_Bookmarked_ShowPinnedOn_ShowBookmarksOff_ReturnsTrue()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned: true);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: true, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: string.Empty,
				showPinned: true,
				showBookmarks: false,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue("pinned record should be visible when ShowPinned is ON");
		}

		[TestMethod]
		public void CanKeep_NoFilters_Pinned_Bookmarked_ShowPinnedOff_ShowBookmarksOn_ReturnsTrue()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned: true);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: true, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: string.Empty,
				showPinned: false,
				showBookmarks: true,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue("bookmarked record should be visible when ShowBookmarks is ON");
		}

		[TestMethod]
		public void CanKeep_NoFilters_Pinned_Bookmarked_ShowPinnedOn_ShowBookmarksOn_ReturnsTrue()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned: true);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: true, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: string.Empty,
				showPinned: true,
				showBookmarks: true,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue("record with both special statuses should be visible when either option is ON");
		}

		#endregion

		#region Include Filter Matches - No Special Records

		[TestMethod]
		public void CanKeep_IncludeMatches_NotPinned_NotBookmarked_ShowPinnedOff_ShowBookmarksOff_ReturnsTrue()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: "ERROR",
				excludeFilter: string.Empty,
				showPinned: false,
				showBookmarks: false,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue("record matches include filter");
		}

		[TestMethod]
		public void CanKeep_IncludeMatches_NotPinned_NotBookmarked_ShowPinnedOn_ShowBookmarksOff_ReturnsTrue()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: "ERROR",
				excludeFilter: string.Empty,
				showPinned: true,
				showBookmarks: false,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue("record matches include filter, regardless of ShowPinned setting");
		}

		[TestMethod]
		public void CanKeep_IncludeMatches_NotPinned_NotBookmarked_ShowPinnedOn_ShowBookmarksOn_ReturnsTrue()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: "ERROR",
				excludeFilter: string.Empty,
				showPinned: true,
				showBookmarks: true,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue("record matches include filter, regardless of show options");
		}

		#endregion

		#region Include Filter No Match - No Special Records

		[TestMethod]
		public void CanKeep_IncludeNoMatch_NotPinned_NotBookmarked_ShowPinnedOff_ShowBookmarksOff_ReturnsFalse()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: "ERROR",
				excludeFilter: string.Empty,
				showPinned: false,
				showBookmarks: false,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeFalse("record does not match include filter");
		}

		[TestMethod]
		public void CanKeep_IncludeNoMatch_NotPinned_NotBookmarked_ShowPinnedOn_ShowBookmarksOff_ReturnsFalse()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: "ERROR",
				excludeFilter: string.Empty,
				showPinned: true,
				showBookmarks: false,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeFalse("record does not match include filter and is not pinned");
		}

		#endregion

		#region Include Filter No Match - Pinned Record

		[TestMethod]
		public void CanKeep_IncludeNoMatch_Pinned_NotBookmarked_ShowPinnedOff_ShowBookmarksOff_ReturnsFalse()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned: true);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: "ERROR",
				excludeFilter: string.Empty,
				showPinned: false,
				showBookmarks: false,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeFalse("record does not match include filter and ShowPinned is OFF");
		}

		[TestMethod]
		public void CanKeep_IncludeNoMatch_Pinned_NotBookmarked_ShowPinnedOn_ShowBookmarksOff_ReturnsTrue()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned: true);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: "ERROR",
				excludeFilter: string.Empty,
				showPinned: true,
				showBookmarks: false,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue("pinned record should be visible when ShowPinned is ON, even if it doesn't match include filter");
		}

		#endregion

		#region Include Filter No Match - Bookmarked Record

		[TestMethod]
		public void CanKeep_IncludeNoMatch_NotPinned_Bookmarked_ShowPinnedOff_ShowBookmarksOn_ReturnsTrue()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: true, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: "ERROR",
				excludeFilter: string.Empty,
				showPinned: false,
				showBookmarks: true,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue("bookmarked record should be visible when ShowBookmarks is ON, even if it doesn't match include filter");
		}

		#endregion

		#region Exclude Filter Matches - No Special Records

		[TestMethod]
		public void CanKeep_ExcludeMatches_NotPinned_NotBookmarked_ShowPinnedOff_ShowBookmarksOff_ReturnsFalse()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: "ERROR",
				showPinned: false,
				showBookmarks: false,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeFalse("record matches exclude filter and should be hidden");
		}

		[TestMethod]
		public void CanKeep_ExcludeMatches_NotPinned_NotBookmarked_ShowPinnedOn_ShowBookmarksOff_ReturnsFalse()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: "ERROR",
				showPinned: true,
				showBookmarks: false,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeFalse("record matches exclude filter, not pinned, and should be hidden");
		}

		[TestMethod]
		public void CanKeep_ExcludeMatches_NotPinned_NotBookmarked_ShowPinnedOff_ShowBookmarksOn_ReturnsFalse()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: "ERROR",
				showPinned: false,
				showBookmarks: true,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeFalse("record matches exclude filter, not bookmarked, and should be hidden");
		}

		[TestMethod]
		public void CanKeep_ExcludeMatches_NotPinned_NotBookmarked_ShowPinnedOn_ShowBookmarksOn_ReturnsFalse()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: "ERROR",
				showPinned: true,
				showBookmarks: true,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeFalse("record matches exclude filter, has no special status, and should be hidden");
		}

		#endregion

		#region Exclude Filter Matches - Pinned Record

		[TestMethod]
		public void CanKeep_ExcludeMatches_Pinned_NotBookmarked_ShowPinnedOff_ShowBookmarksOff_ReturnsFalse()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_MATCH, SAMPLE_LINE_NUMBER, isPinned: true);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: "ERROR",
				showPinned: false,
				showBookmarks: false,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeFalse("record matches exclude filter and ShowPinned is OFF, so it should be hidden");
		}

		[TestMethod]
		public void CanKeep_ExcludeMatches_Pinned_NotBookmarked_ShowPinnedOn_ShowBookmarksOff_ReturnsTrue()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_MATCH, SAMPLE_LINE_NUMBER, isPinned: true);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: "ERROR",
				showPinned: true,
				showBookmarks: false,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue("pinned record with ShowPinned ON should be visible, even if it matches exclude filter");
		}

		#endregion

		#region Exclude Filter Matches - Bookmarked Record

		[TestMethod]
		public void CanKeep_ExcludeMatches_NotPinned_Bookmarked_ShowPinnedOff_ShowBookmarksOff_ReturnsFalse()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: true, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: "ERROR",
				showPinned: false,
				showBookmarks: false,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeFalse("record matches exclude filter and ShowBookmarks is OFF, so it should be hidden");
		}

		[TestMethod]
		public void CanKeep_ExcludeMatches_NotPinned_Bookmarked_ShowPinnedOff_ShowBookmarksOn_ReturnsTrue()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: true, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: "ERROR",
				showPinned: false,
				showBookmarks: true,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue("bookmarked record with ShowBookmarks ON should be visible, even if it matches exclude filter");
		}

		#endregion

		#region Exclude Filter No Match - No Special Records

		[TestMethod]
		public void CanKeep_ExcludeNoMatch_NotPinned_NotBookmarked_ShowPinnedOff_ShowBookmarksOff_ReturnsTrue()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: "ERROR",
				showPinned: false,
				showBookmarks: false,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue("record does not match exclude filter, so it should be visible");
		}

		[TestMethod]
		public void CanKeep_ExcludeNoMatch_NotPinned_NotBookmarked_ShowPinnedOn_ShowBookmarksOff_ReturnsFalse()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: "ERROR",
				showPinned: true,
				showBookmarks: false,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeFalse("with ShowPinned ON and no include filter, only pinned records should be visible");
		}

		[TestMethod]
		public void CanKeep_ExcludeNoMatch_NotPinned_NotBookmarked_ShowPinnedOff_ShowBookmarksOn_ReturnsFalse()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: "ERROR",
				showPinned: false,
				showBookmarks: true,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeFalse("with ShowBookmarks ON and no include filter, only bookmarked records should be visible");
		}

		[TestMethod]
		public void CanKeep_ExcludeNoMatch_NotPinned_NotBookmarked_ShowPinnedOn_ShowBookmarksOn_ReturnsFalse()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,
				excludeFilter: "ERROR",
				showPinned: true,
				showBookmarks: true,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeFalse("with both options ON and no include filter, only special records should be visible");
		}

		#endregion

		#region Include and Exclude Both Match - No Special Records

		[TestMethod]
		public void CanKeep_BothMatch_NotPinned_NotBookmarked_ShowPinnedOff_ShowBookmarksOff_ReturnsFalse()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: "ERROR",
				excludeFilter: "ERROR",
				showPinned: false,
				showBookmarks: false,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeFalse("exclude filter takes priority over include filter");
		}

		[TestMethod]
		public void CanKeep_BothMatch_Pinned_NotBookmarked_ShowPinnedOn_ShowBookmarksOff_ReturnsTrue()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_MATCH, SAMPLE_LINE_NUMBER, isPinned: true);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: "ERROR",
				excludeFilter: "ERROR",
				showPinned: true,
				showBookmarks: false,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue("pinned record with ShowPinned ON should override both filters");
		}

		[TestMethod]
		public void CanKeep_BothMatch_NotPinned_Bookmarked_ShowPinnedOff_ShowBookmarksOn_ReturnsTrue()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: true, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: "ERROR",
				excludeFilter: "ERROR",
				showPinned: false,
				showBookmarks: true,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue("bookmarked record with ShowBookmarks ON should override both filters");
		}

		#endregion

		#region Include Matches, Exclude No Match

		[TestMethod]
		public void CanKeep_IncludeMatchesExcludeNoMatch_NotPinned_NotBookmarked_ShowPinnedOff_ShowBookmarksOff_ReturnsTrue()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: "ERROR",
				excludeFilter: "DEBUG",
				showPinned: false,
				showBookmarks: false,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue("record matches include filter and does not match exclude filter");
		}

		#endregion

		#region Include No Match, Exclude Matches

		[TestMethod]
		public void CanKeep_IncludeNoMatchExcludeMatches_NotPinned_NotBookmarked_ShowPinnedOff_ShowBookmarksOff_ReturnsFalse()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: "ERROR",
				excludeFilter: "INFO",
				showPinned: false,
				showBookmarks: false,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeFalse("record does not match include filter");
		}

		#endregion
	}
}
