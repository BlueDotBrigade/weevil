using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BlueDotBrigade.Weevil.Core;

namespace BlueDotBrigade.Weevil.Core.UnitTests
{
    [TestClass]
    public class RegionManagerTests
    {
        private BookendManager _bookendManager;

        [TestInitialize]
        public void Setup()
        {
            _bookendManager = new BookendManager();
        }

        [TestMethod]
        public void MarkEnd_AfterStart_BookendCreated()
        {
            // Arrange
            int startIndex = 800;
            int endIndex = 900;
            _bookendManager.MarkStart(startIndex);

            // Act
            _bookendManager.MarkEnd(endIndex);

            // Assert
            Assert.AreEqual(1, _bookendManager.Bookends.Length);
            Assert.AreEqual(startIndex, _bookendManager.Bookends[0].StartLineNumber);
            Assert.AreEqual(endIndex, _bookendManager.Bookends[0].EndLineNumber);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void MarkEnd_WithoutStart_ThrowsInvalidOperationException()
        {
            // Arrange
            int endIndex = 900;

            // Act
            _bookendManager.MarkEnd(endIndex);

            // Assert handled by ExpectedException
        }

        [TestMethod]
        public void Bookends_CreateMultipleBookends_ReturnsAllBookends()
        {
            // Arrange
            _bookendManager.MarkStart(800);
            _bookendManager.MarkEnd(900);

            _bookendManager.MarkStart(950);
            _bookendManager.MarkEnd(1000);

            // Act
            // Assert
            Assert.AreEqual(2, _bookendManager.Bookends.Length);
            Assert.AreEqual(800, _bookendManager.Bookends[0].StartLineNumber);
            Assert.AreEqual(900, _bookendManager.Bookends[0].EndLineNumber);
            Assert.AreEqual(950, _bookendManager.Bookends[1].StartLineNumber);
            Assert.AreEqual(1000, _bookendManager.Bookends[1].EndLineNumber);
        }

        [TestMethod]
        public void Clear_ExistingBookends_AllBookendsDeleted()
        {
            // Arrange
            _bookendManager.MarkStart(800);
            _bookendManager.MarkEnd(900);

            // Act
            _bookendManager.Clear();

            // Assert
            Assert.AreEqual(0, _bookendManager.Bookends.Length);
        }

        [TestMethod]
        public void Clear_IndexIsWithinBookend_BookendDeleted()
        {
            // Arrange
            _bookendManager.MarkStart(800);
            _bookendManager.MarkEnd(900);

            // Act
            _bookendManager.Clear(850);

            // Assert
            Assert.AreEqual(0, _bookendManager.Bookends.Length);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Clear_LineNumberIsOutsideOfBookend_Throws()
        {
            // Arrange
            _bookendManager.MarkStart(800);
            _bookendManager.MarkEnd(900);

            // Act
            _bookendManager.Clear(100);

            // Assert
            Assert.AreEqual(1, _bookendManager.Bookends.Length);
        }
    }
}