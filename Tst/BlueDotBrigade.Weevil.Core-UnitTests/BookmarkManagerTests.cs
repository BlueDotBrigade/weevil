using System;
using BlueDotBrigade.Weevil.Core;

namespace BlueDotBrigade.Weevil.Core.UnitTests
{
        [TestClass]
        public class BookmarkManagerTests
        {
                private BookmarkManager _bookmarkManager;

                [TestInitialize]
                public void Setup()
                {
                        _bookmarkManager = new BookmarkManager();
                }

                [TestMethod]
                public void CreateFromSelection_SingleBookmark_BookmarkCreated()
                {
                        // Arrange
                        int lineNumber = 42;

                        // Act
                        _bookmarkManager.CreateFromSelection(0, "Test", lineNumber);

                        // Assert
                        _bookmarkManager.Bookmarks.Length.Should().Be(1);
                        _bookmarkManager.Bookmarks[0].Name.Should().Be("Test");
                        _bookmarkManager.Bookmarks[0].Record.LineNumber.Should().Be(lineNumber);
                }

                [TestMethod]
                public void CreateFromSelection_DuplicateLine_ThrowsInvalidOperationException()
                {
                        // Arrange
                        int lineNumber = 42;
                        _bookmarkManager.CreateFromSelection(0, "A", lineNumber);

                        // Act
                        Action act = () => _bookmarkManager.CreateFromSelection(0, "B", lineNumber);

                        // Assert
                        act.Should().Throw<InvalidOperationException>();
                }

                [TestMethod]
                public void Clear_LineNumber_RemovesBookmark()
                {
                        // Arrange
                        int lineNumber = 42;
                        _bookmarkManager.CreateFromSelection(0, "A", lineNumber);

                        // Act
                        var wasCleared = _bookmarkManager.Clear(lineNumber);

                        // Assert
                        wasCleared.Should().BeTrue();
                        _bookmarkManager.Bookmarks.Length.Should().Be(0);
                }

                [TestMethod]
                public void Clear_All_RemovesBookmarks()
                {
                        // Arrange
                        _bookmarkManager.CreateFromSelection(0, "A", 1);
                        _bookmarkManager.CreateFromSelection(0, "B", 2);

                        // Act
                        _bookmarkManager.Clear();

                        // Assert
                        _bookmarkManager.Bookmarks.Length.Should().Be(0);
                }

                [TestMethod]
                public void Clear_All_ResetsSequenceNumber()
                {
                        // Arrange
                        _bookmarkManager.CreateFromSelection(0, string.Empty, 1);  // Creates "1"
                        _bookmarkManager.CreateFromSelection(0, string.Empty, 2);  // Creates "2"
                        _bookmarkManager.Clear();

                        // Act
                        _bookmarkManager.CreateFromSelection(0, string.Empty, 3);

                        // Assert - Should restart from 1 after clear
                        _bookmarkManager.Bookmarks.Length.Should().Be(1);
                        _bookmarkManager.Bookmarks[0].Name.Should().Be("1");
                }

                [TestMethod]
                public void Clear_SpecificBookmark_RecalculatesSequence()
                {
                        // Arrange
                        _bookmarkManager.CreateFromSelection(0, "1", 10);
                        _bookmarkManager.CreateFromSelection(0, "2", 20);
                        _bookmarkManager.CreateFromSelection(0, "3", 30);

                        // Act - Remove bookmark "2"
                        _bookmarkManager.Clear(20);
                        _bookmarkManager.CreateFromSelection(0, string.Empty, 40);

                        // Assert - Sequence should continue from max remaining (3) + 1 = 4
                        _bookmarkManager.Bookmarks.Length.Should().Be(3);
                        _bookmarkManager.Bookmarks[2].Name.Should().Be("4");
                }

                [TestMethod]
                public void Clear_AllSequentialBookmarks_ResetsToOne()
                {
                        // Arrange
                        _bookmarkManager.CreateFromSelection(0, "1", 10);
                        _bookmarkManager.CreateFromSelection(0, "2", 20);
                        _bookmarkManager.CreateFromSelection(0, "3", 30);

                        // Act - Remove all sequential bookmarks one by one
                        _bookmarkManager.Clear(30);  // Remove "3"
                        _bookmarkManager.Clear(20);  // Remove "2"
                        _bookmarkManager.Clear(10);  // Remove "1"
                        _bookmarkManager.CreateFromSelection(0, string.Empty, 40);

                        // Assert - Should restart from 1 when all numeric bookmarks are removed
                        _bookmarkManager.Bookmarks.Length.Should().Be(1);
                        _bookmarkManager.Bookmarks[0].Name.Should().Be("1");
                }

                [TestMethod]
                public void CreateFromSelection_EmptyName_UsesSequentialNumber()
                {
                        // Arrange & Act
                        _bookmarkManager.CreateFromSelection(0, string.Empty, 10);
                        _bookmarkManager.CreateFromSelection(0, string.Empty, 20);
                        _bookmarkManager.CreateFromSelection(0, string.Empty, 30);

                        // Assert
                        _bookmarkManager.Bookmarks.Length.Should().Be(3);
                        _bookmarkManager.Bookmarks[0].Name.Should().Be("1");
                        _bookmarkManager.Bookmarks[1].Name.Should().Be("2");
                        _bookmarkManager.Bookmarks[2].Name.Should().Be("3");
                }

                [TestMethod]
                public void CreateFromSelection_NullName_UsesSequentialNumber()
                {
                        // Arrange & Act
                        _bookmarkManager.CreateFromSelection(0, null, 10);

                        // Assert
                        _bookmarkManager.Bookmarks.Length.Should().Be(1);
                        _bookmarkManager.Bookmarks[0].Name.Should().Be("1");
                }

                [TestMethod]
                public void CreateFromSelection_MixedNamesAndSequential_MaintainsSequence()
                {
                        // Arrange & Act
                        _bookmarkManager.CreateFromSelection(0, "custom1", 10);
                        _bookmarkManager.CreateFromSelection(0, string.Empty, 20);  // Should be "1"
                        _bookmarkManager.CreateFromSelection(0, "custom2", 30);
                        _bookmarkManager.CreateFromSelection(0, string.Empty, 40);  // Should be "2"

                        // Assert
                        _bookmarkManager.Bookmarks.Length.Should().Be(4);
                        _bookmarkManager.Bookmarks[0].Name.Should().Be("custom1");
                        _bookmarkManager.Bookmarks[1].Name.Should().Be("1");
                        _bookmarkManager.Bookmarks[2].Name.Should().Be("custom2");
                        _bookmarkManager.Bookmarks[3].Name.Should().Be("2");
                }

                [TestMethod]
                public void Constructor_WithExistingSequentialBookmarks_ContinuesSequence()
                {
                        // Arrange - Create bookmarks with sequential numbers
                        _bookmarkManager.CreateFromSelection(0, "1", 10);
                        _bookmarkManager.CreateFromSelection(0, "2", 20);
                        _bookmarkManager.CreateFromSelection(0, "3", 30);

                        var existingBookmarks = _bookmarkManager.Bookmarks;

                        // Act - Create new manager with existing bookmarks
                        var newManager = new BookmarkManager(existingBookmarks);
                        newManager.CreateFromSelection(0, string.Empty, 40);

                        // Assert - Should continue from 4
                        newManager.Bookmarks.Length.Should().Be(4);
                        newManager.Bookmarks[3].Name.Should().Be("4");
                }

                [TestMethod]
                public void Constructor_WithNonSequentialBookmarks_StartsSequenceFromOne()
                {
                        // Arrange - Create bookmarks with non-sequential names
                        _bookmarkManager.CreateFromSelection(0, "foo", 10);
                        _bookmarkManager.CreateFromSelection(0, "bar", 20);

                        var existingBookmarks = _bookmarkManager.Bookmarks;

                        // Act - Create new manager with existing bookmarks
                        var newManager = new BookmarkManager(existingBookmarks);
                        newManager.CreateFromSelection(0, string.Empty, 30);

                        // Assert - Should start from 1
                        newManager.Bookmarks.Length.Should().Be(3);
                        newManager.Bookmarks[2].Name.Should().Be("1");
                }

                [TestMethod]
                public void CreateFromSelection_WithId_StoresId()
                {
                        // Arrange & Act
                        _bookmarkManager.CreateFromSelection(4, "Test", 100);

                        // Assert
                        _bookmarkManager.Bookmarks.Length.Should().Be(1);
                        _bookmarkManager.Bookmarks[0].Id.Should().Be(4);
                        _bookmarkManager.Bookmarks[0].Name.Should().Be("Test");
                }

                [TestMethod]
                public void TryGetBookmarkById_ExistingId_ReturnsTrue()
                {
                        // Arrange
                        _bookmarkManager.CreateFromSelection(3, "MyBookmark", 50);

                        // Act
                        var found = _bookmarkManager.TryGetBookmarkById(3, out var bookmark);

                        // Assert
                        found.Should().BeTrue();
                        bookmark.Should().NotBeNull();
                        bookmark.Id.Should().Be(3);
                        bookmark.Name.Should().Be("MyBookmark");
                        bookmark.Record.LineNumber.Should().Be(50);
                }

                [TestMethod]
                public void TryGetBookmarkById_NonExistingId_ReturnsFalse()
                {
                        // Arrange
                        _bookmarkManager.CreateFromSelection(1, "Test", 10);

                        // Act
                        var found = _bookmarkManager.TryGetBookmarkById(5, out var bookmark);

                        // Assert
                        found.Should().BeFalse();
                        bookmark.Should().BeNull();
                }

                [TestMethod]
                public void CreateFromSelection_DuplicateId_ReplacesExistingBookmark()
                {
                        // Arrange - Create a bookmark with ID 2
                        _bookmarkManager.CreateFromSelection(2, "First", 10);

                        // Act - Create another bookmark with the same ID 2, but different line
                        _bookmarkManager.CreateFromSelection(2, "Second", 20);

                        // Assert - The old bookmark should be replaced; only one bookmark with ID=2 should exist
                        _bookmarkManager.Bookmarks.Length.Should().Be(1);
                        var found = _bookmarkManager.TryGetBookmarkById(2, out var bookmark);
                        found.Should().BeTrue();
                        bookmark.Name.Should().Be("Second");
                        bookmark.Record.LineNumber.Should().Be(20);
                }

                [TestMethod]
                public void TryGetBookmark_ExistingLineNumber_ReturnsBookmark()
                {
                        // Arrange
                        _bookmarkManager.CreateFromSelection(3, "Test", 100);

                        // Act
                        var found = _bookmarkManager.TryGetBookmark(100, out var bookmark);

                        // Assert
                        found.Should().BeTrue();
                        bookmark.Should().NotBeNull();
                        bookmark.Id.Should().Be(3);
                        bookmark.Name.Should().Be("Test");
                }

                [TestMethod]
                public void TryGetBookmark_NonExistingLineNumber_ReturnsFalse()
                {
                        // Arrange
                        _bookmarkManager.CreateFromSelection(1, "Test", 10);

                        // Act
                        var found = _bookmarkManager.TryGetBookmark(999, out var bookmark);

                        // Assert
                        found.Should().BeFalse();
                        bookmark.Should().BeNull();
                }
        }
}
