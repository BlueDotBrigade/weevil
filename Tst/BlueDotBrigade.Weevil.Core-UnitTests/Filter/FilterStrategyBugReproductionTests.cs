using System.Collections.Concurrent;
using BlueDotBrigade.Weevil.Data;
using BlueDotBrigade.Weevil.Filter;
using NSubstitute;

namespace BlueDotBrigade.Weevil.Core.UnitTests.Filter
{
	/// <summary>
	/// Tests to reproduce the filtering bug where ShowPinned/ShowBookmarks
	/// incorrectly hide records when only an exclude filter is present.
	/// 
	/// Bug: When ShowPinned or ShowBookmarks is ON with ONLY an exclude filter,
	/// the system incorrectly hides non-special records that don't match the exclude filter.
	/// 
	/// Expected: When an exclude filter is present, all non-excluded records should be shown,
	/// and ShowPinned/ShowBookmarks should ONLY add back special records that were excluded.
	/// </summary>
	[TestClass]
	public class FilterStrategyBugReproductionTests
	{
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

		/// <summary>
		/// BUG REPRODUCTION:
		/// When ONLY an exclude filter exists (no include filter), and ShowPinned is ON,
		/// a regular record that doesn't match the exclude filter should still be visible.
		/// 
		/// Current (buggy) behavior: Returns false (hides the record)
		/// Expected behavior: Returns true (shows the record)
		/// 
		/// Reasoning: The exclude filter says "hide ERROR records". This INFO record doesn't
		/// match the exclude filter, so it should be shown. The ShowPinned option should only
		/// affect whether pinned records that DO match the exclude filter are shown anyway.
		/// </summary>
		[TestMethod]
		public void BugReproduction_ExcludeFilterWithShowPinned_RegularRecordNotMatchingExclude_ShouldBeVisible()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,  // No include filter
				excludeFilter: "ERROR",        // Exclude ERROR records
				showPinned: true,              // Show pinned records even if excluded
				showBookmarks: false,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue(
				"When only exclude filter exists, non-excluded records should be visible regardless of ShowPinned setting. " +
				"ShowPinned should only affect whether excluded pinned records are shown anyway.");
		}

		/// <summary>
		/// BUG REPRODUCTION:
		/// When ONLY an exclude filter exists (no include filter), and ShowBookmarks is ON,
		/// a regular record that doesn't match the exclude filter should still be visible.
		/// 
		/// Current (buggy) behavior: Returns false (hides the record)
		/// Expected behavior: Returns true (shows the record)
		/// </summary>
		[TestMethod]
		public void BugReproduction_ExcludeFilterWithShowBookmarks_RegularRecordNotMatchingExclude_ShouldBeVisible()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_NO_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,  // No include filter
				excludeFilter: "ERROR",        // Exclude ERROR records
				showPinned: false,
				showBookmarks: true,           // Show bookmarked records even if excluded
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue(
				"When only exclude filter exists, non-excluded records should be visible regardless of ShowBookmarks setting. " +
				"ShowBookmarks should only affect whether excluded bookmarked records are shown anyway.");
		}

		/// <summary>
		/// This test should pass - it verifies that pinned records excluded by the filter
		/// are still shown when ShowPinned is ON.
		/// </summary>
		[TestMethod]
		public void ExcludeFilterWithShowPinned_PinnedRecordMatchingExclude_ShouldBeVisible()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_MATCH, SAMPLE_LINE_NUMBER, isPinned: true);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: false, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,  // No include filter
				excludeFilter: "ERROR",        // Exclude ERROR records
				showPinned: true,              // Show pinned records even if excluded
				showBookmarks: false,
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue(
				"Pinned record matching exclude filter should be visible when ShowPinned is ON");
		}

		/// <summary>
		/// This test should pass - it verifies that bookmarked records excluded by the filter
		/// are still shown when ShowBookmarks is ON.
		/// </summary>
		[TestMethod]
		public void ExcludeFilterWithShowBookmarks_BookmarkedRecordMatchingExclude_ShouldBeVisible()
		{
			// Arrange
			var record = CreateRecord(SAMPLE_CONTENT_MATCH, SAMPLE_LINE_NUMBER, isPinned: false);
			var bookmarkManager = CreateBookmarkManager(hasBookmark: true, SAMPLE_LINE_NUMBER);
			var strategy = CreateFilterStrategy(
				includeFilter: string.Empty,  // No include filter
				excludeFilter: "ERROR",        // Exclude ERROR records
				showPinned: false,
				showBookmarks: true,           // Show bookmarked records even if excluded
				bookmarkManager);

			// Act
			var result = strategy.CanKeep(record);

			// Assert
			result.Should().BeTrue(
				"Bookmarked record matching exclude filter should be visible when ShowBookmarks is ON");
		}
	}
}
