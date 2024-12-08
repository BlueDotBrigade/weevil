using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BlueDotBrigade.Weevil.Core;

namespace BlueDotBrigade.Weevil.Core.UnitTests
{
	[TestClass]
	public class BookendManagerTests
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
			_bookendManager.Bookends.Length.Should().Be(1);
			_bookendManager.Bookends[0].StartLineNumber.Should().Be(startIndex);
			_bookendManager.Bookends[0].EndLineNumber.Should().Be(endIndex);
		}

		[TestMethod]
		public void MarkEnd_WithoutStart_ThrowsInvalidOperationException()
		{
			// Arrange
			int endIndex = 900;

			// Act
			Action act = () => _bookendManager.MarkEnd(endIndex);

			// Assert
			act.Should().Throw<InvalidOperationException>();
		}

		[TestMethod]
		public void Bookends_CreateMultipleBookends_ReturnsAllBookends()
		{
			// Arrange
			_bookendManager.MarkStart(800);
			_bookendManager.MarkEnd(900);

			_bookendManager.MarkStart(950);
			_bookendManager.MarkEnd(1000);

			// Act & Assert
			_bookendManager.Bookends.Length.Should().Be(2);
			_bookendManager.Bookends[0].StartLineNumber.Should().Be(800);
			_bookendManager.Bookends[0].EndLineNumber.Should().Be(900);
			_bookendManager.Bookends[1].StartLineNumber.Should().Be(950);
			_bookendManager.Bookends[1].EndLineNumber.Should().Be(1000);
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
			_bookendManager.Bookends.Length.Should().Be(0);
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
			_bookendManager.Bookends.Length.Should().Be(0);
		}

		[TestMethod]
		public void Clear_LineNumberIsOutsideOfBookend_ReturnsFalseAndKeepsBookend()
		{
			// Arrange
			_bookendManager.MarkStart(800);
			_bookendManager.MarkEnd(900);

			// Act
			var wasCleared = _bookendManager.Clear(100);

			// Assert
			wasCleared.Should().BeFalse();
			_bookendManager.Bookends.Length.Should().Be(1);
		}
	}
}
