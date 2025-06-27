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
        }
}
