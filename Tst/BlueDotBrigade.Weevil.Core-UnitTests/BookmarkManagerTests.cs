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
                        _bookmarkManager.CreateFromSelection("Test", lineNumber);

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
                        _bookmarkManager.CreateFromSelection("A", lineNumber);

                        // Act
                        Action act = () => _bookmarkManager.CreateFromSelection("B", lineNumber);

                        // Assert
                        act.Should().Throw<InvalidOperationException>();
                }

                [TestMethod]
                public void Clear_LineNumber_RemovesBookmark()
                {
                        // Arrange
                        int lineNumber = 42;
                        _bookmarkManager.CreateFromSelection("A", lineNumber);

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
                        _bookmarkManager.CreateFromSelection("A", 1);
                        _bookmarkManager.CreateFromSelection("B", 2);

                        // Act
                        _bookmarkManager.Clear();

                        // Assert
                        _bookmarkManager.Bookmarks.Length.Should().Be(0);
                }

                [TestMethod]
                public void Clear_All_ResetsSequenceNumber()
                {
                        // Arrange
                        _bookmarkManager.CreateFromSelection(string.Empty, 1);  // Creates "1"
                        _bookmarkManager.CreateFromSelection(string.Empty, 2);  // Creates "2"
                        _bookmarkManager.Clear();

                        // Act
                        _bookmarkManager.CreateFromSelection(string.Empty, 3);

                        // Assert - Should restart from 1 after clear
                        _bookmarkManager.Bookmarks.Length.Should().Be(1);
                        _bookmarkManager.Bookmarks[0].Name.Should().Be("1");
                }

                [TestMethod]
                public void Clear_SpecificBookmark_RecalculatesSequence()
                {
                        // Arrange
                        _bookmarkManager.CreateFromSelection("1", 10);
                        _bookmarkManager.CreateFromSelection("2", 20);
                        _bookmarkManager.CreateFromSelection("3", 30);

                        // Act - Remove bookmark "2"
                        _bookmarkManager.Clear(20);
                        _bookmarkManager.CreateFromSelection(string.Empty, 40);

                        // Assert - Sequence should continue from max remaining (3) + 1 = 4
                        _bookmarkManager.Bookmarks.Length.Should().Be(3);
                        _bookmarkManager.Bookmarks[2].Name.Should().Be("4");
                }

                [TestMethod]
                public void Clear_AllSequentialBookmarks_ResetsToOne()
                {
                        // Arrange
                        _bookmarkManager.CreateFromSelection("1", 10);
                        _bookmarkManager.CreateFromSelection("2", 20);
                        _bookmarkManager.CreateFromSelection("3", 30);

                        // Act - Remove all sequential bookmarks one by one
                        _bookmarkManager.Clear(30);  // Remove "3"
                        _bookmarkManager.Clear(20);  // Remove "2"
                        _bookmarkManager.Clear(10);  // Remove "1"
                        _bookmarkManager.CreateFromSelection(string.Empty, 40);

                        // Assert - Should restart from 1 when all numeric bookmarks are removed
                        _bookmarkManager.Bookmarks.Length.Should().Be(1);
                        _bookmarkManager.Bookmarks[0].Name.Should().Be("1");
                }

                [TestMethod]
                public void CreateFromSelection_EmptyName_UsesSequentialNumber()
                {
                        // Arrange & Act
                        _bookmarkManager.CreateFromSelection(string.Empty, 10);
                        _bookmarkManager.CreateFromSelection(string.Empty, 20);
                        _bookmarkManager.CreateFromSelection(string.Empty, 30);

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
                        _bookmarkManager.CreateFromSelection(null, 10);

                        // Assert
                        _bookmarkManager.Bookmarks.Length.Should().Be(1);
                        _bookmarkManager.Bookmarks[0].Name.Should().Be("1");
                }

                [TestMethod]
                public void CreateFromSelection_MixedNamesAndSequential_MaintainsSequence()
                {
                        // Arrange & Act
                        _bookmarkManager.CreateFromSelection("custom1", 10);
                        _bookmarkManager.CreateFromSelection(string.Empty, 20);  // Should be "1"
                        _bookmarkManager.CreateFromSelection("custom2", 30);
                        _bookmarkManager.CreateFromSelection(string.Empty, 40);  // Should be "2"

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
                        _bookmarkManager.CreateFromSelection("1", 10);
                        _bookmarkManager.CreateFromSelection("2", 20);
                        _bookmarkManager.CreateFromSelection("3", 30);

                        var existingBookmarks = _bookmarkManager.Bookmarks;

                        // Act - Create new manager with existing bookmarks
                        var newManager = new BookmarkManager(existingBookmarks);
                        newManager.CreateFromSelection(string.Empty, 40);

                        // Assert - Should continue from 4
                        newManager.Bookmarks.Length.Should().Be(4);
                        newManager.Bookmarks[3].Name.Should().Be("4");
                }

                [TestMethod]
                public void Constructor_WithNonSequentialBookmarks_StartsSequenceFromOne()
                {
                        // Arrange - Create bookmarks with non-sequential names
                        _bookmarkManager.CreateFromSelection("foo", 10);
                        _bookmarkManager.CreateFromSelection("bar", 20);

                        var existingBookmarks = _bookmarkManager.Bookmarks;

                        // Act - Create new manager with existing bookmarks
                        var newManager = new BookmarkManager(existingBookmarks);
                        newManager.CreateFromSelection(string.Empty, 30);

                        // Assert - Should start from 1
                        newManager.Bookmarks.Length.Should().Be(3);
                        newManager.Bookmarks[2].Name.Should().Be("1");
                }
        }
}
