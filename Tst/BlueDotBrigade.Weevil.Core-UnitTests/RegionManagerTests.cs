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
	public class RegionManagerTests
	{
		private ISelect _selectionManager;
		private RegionManager _regionManager;

		[TestInitialize]
		public void Setup()
		{
			_selectionManager = Substitute.For<ISelect>();
			_regionManager = new RegionManager(_selectionManager);
		}

		[TestMethod]
		public void MarkEnd_AfterStart_RegionCreated()
		{
			// Arrange
			int minLineNumber = 800;
			int maxLineNumber = 900;
			_regionManager.MarkStart(minLineNumber);

			// Act
			_regionManager.MarkEnd(maxLineNumber);

			// Assert
			_regionManager.Regions.Length.Should().Be(1);
			_regionManager.Regions[0].Minimum.LineNumber.Should().Be(minLineNumber);
			_regionManager.Regions[0].Maximum.LineNumber.Should().Be(maxLineNumber);
		}

		[TestMethod]
		public void MarkEnd_WithoutStart_ThrowsInvalidOperationException()
		{
			// Arrange
			int maxLineNumber = 900;

			// Act
			Action act = () => _regionManager.MarkEnd(maxLineNumber);

			// Assert
			act.Should().Throw<InvalidOperationException>();
		}

		[TestMethod]
		public void Regions_CreateMultipleRegions_ReturnsAllRegions()
		{
			// Arrange
			_regionManager.MarkStart(800);
			_regionManager.MarkEnd(900);

			_regionManager.MarkStart(950);
			_regionManager.MarkEnd(1000);

			// Act & Assert
			_regionManager.Regions.Length.Should().Be(2);
			_regionManager.Regions[0].Minimum.LineNumber.Should().Be(800);
			_regionManager.Regions[0].Maximum.LineNumber.Should().Be(900);
			_regionManager.Regions[1].Minimum.LineNumber.Should().Be(950);
			_regionManager.Regions[1].Maximum.LineNumber.Should().Be(1000);
		}

		[TestMethod]
		public void Clear_ExistingRegions_AllRegionsDeleted()
		{
			// Arrange
			_regionManager.MarkStart(800);
			_regionManager.MarkEnd(900);

			// Act
			_regionManager.Clear();

			// Assert
			_regionManager.Regions.Length.Should().Be(0);
		}

		[TestMethod]
		public void Clear_IndexIsWithinRegion_RegionDeleted()
		{
			// Arrange
			_regionManager.MarkStart(800);
			_regionManager.MarkEnd(900);

			// Act
			_regionManager.Clear(850);

			// Assert
			_regionManager.Regions.Length.Should().Be(0);
		}

		[TestMethod]
		public void Clear_LineNumberIsOutsideOfRegion_ReturnsFalseAndKeepsRegions()
		{
			// Arrange
			_regionManager.MarkStart(800);
			_regionManager.MarkEnd(900);

			// Act
			var wasCleared = _regionManager.Clear(100);

			// Assert
			wasCleared.Should().BeFalse();
			_regionManager.Bookends.Length.Should().Be(1);
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
			_regionManager.CreateFromSelection();

			// Assert
			_regionManager.Bookends.Length.Should().Be(1);
			_regionManager.Bookends[0].Minimum.LineNumber.Should().Be(16);
			_regionManager.Bookends[0].Maximum.LineNumber.Should().Be(32);
		}
	}
}