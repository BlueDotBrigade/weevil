using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BlueDotBrigade.Weevil.Core;
using NSubstitute;
using System.Collections.Generic;
using BlueDotBrigade.Weevil.Data;
using System.Linq;

namespace BlueDotBrigade.Weevil.Core.UnitTests
{
	[TestClass]
	public class BookendManagerTests
	{
		private ISelect _selectionManager;
		private BookendManager _bookendManager;

		[TestInitialize]
		public void Setup()
		{
			_selectionManager = Substitute.For<ISelect>();
			_bookendManager = new BookendManager(_selectionManager);
		}

		[TestMethod]
		public void MarkEnd_AfterStart_BookendCreated()
		{
			// Arrange
			int minLineNumber = 800;
			int maxLineNumber = 900;
			_bookendManager.MarkStart(minLineNumber);

			// Act
			_bookendManager.MarkEnd(maxLineNumber);

			// Assert
			_bookendManager.Bookends.Length.Should().Be(1);
			_bookendManager.Bookends[0].Minimum.LineNumber.Should().Be(minLineNumber);
			_bookendManager.Bookends[0].Maximum.LineNumber.Should().Be(maxLineNumber);
		}

		[TestMethod]
		public void MarkEnd_WithoutStart_ThrowsInvalidOperationException()
		{
			// Arrange
			int maxLineNumber = 900;

			// Act
			Action act = () => _bookendManager.MarkEnd(maxLineNumber);

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
			_bookendManager.Bookends[0].Minimum.LineNumber.Should().Be(800);
			_bookendManager.Bookends[0].Maximum.LineNumber.Should().Be(900);
			_bookendManager.Bookends[1].Minimum.LineNumber.Should().Be(950);
			_bookendManager.Bookends[1].Maximum.LineNumber.Should().Be(1000);
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

		[TestMethod]
		public void CreateFromSelection_LineNumberIsOutsideOfBookend_ReturnsFalseAndKeepsBookend()
		{
			// Arrange
			var selectedRecords = Enumerable
				.Range(start: 16, count: 17)
				.ToDictionary(lineNumber => lineNumber, lineNumber => R.WithLineNumber(lineNumber));

			_selectionManager.Selected.Returns(selectedRecords);
			_selectionManager.HasSelectionPeriod.Returns(true);

			// Act
			_bookendManager.CreateFromSelection();

			// Assert
			_bookendManager.Bookends.Length.Should().Be(1);
			_bookendManager.Bookends[0].Minimum.LineNumber.Should().Be(16);
			_bookendManager.Bookends[0].Maximum.LineNumber.Should().Be(32);
		}
	}
}