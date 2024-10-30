using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BlueDotBrigade.Weevil.Core;

namespace BlueDotBrigade.Weevil.Core.UnitTests
{
    [TestClass]
    public class RegionManagerTests
    {
        private RegionManager _regionManager;

        [TestInitialize]
        public void Setup()
        {
            _regionManager = new RegionManager();
        }

        [TestMethod]
        public void MarkRegionEnd_AfterStartRegion_RegionAdded()
        {
            // Arrange
            int startIndex = 800;
            int endIndex = 900;
            _regionManager.MarkStart(startIndex);

            // Act
            _regionManager.MarkEnd(endIndex);

            // Assert
            Assert.AreEqual(1, _regionManager.Regions.Length);
            Assert.AreEqual(startIndex, _regionManager.Regions[0].StartLineNumber);
            Assert.AreEqual(endIndex, _regionManager.Regions[0].EndLineNumber);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void MarkRegionEnd_WithoutStart_ThrowsInvalidOperationException()
        {
            // Arrange
            int endIndex = 900;

            // Act
            _regionManager.MarkEnd(endIndex);

            // Assert handled by ExpectedException
        }

        [TestMethod]
        public void GetRegionsOfInterest_AfterMultipleRegions_ReturnsAllRegions()
        {
            // Arrange
            _regionManager.MarkStart(800);
            _regionManager.MarkEnd(900);

            _regionManager.MarkStart(950);
            _regionManager.MarkEnd(1000);

            // Act
            // Assert
            Assert.AreEqual(2, _regionManager.Regions.Length);
            Assert.AreEqual(800, _regionManager.Regions[0].StartLineNumber);
            Assert.AreEqual(900, _regionManager.Regions[0].EndLineNumber);
            Assert.AreEqual(950, _regionManager.Regions[1].StartLineNumber);
            Assert.AreEqual(1000, _regionManager.Regions[1].EndLineNumber);
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
            Assert.AreEqual(0, _regionManager.Regions.Length);
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
            Assert.AreEqual(0, _regionManager.Regions.Length);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Clear_IndexIsOutsideOfRegion_Throws()
        {
            // Arrange
            _regionManager.MarkStart(800);
            _regionManager.MarkEnd(900);

            // Act
            _regionManager.Clear(100);

            // Assert
            Assert.AreEqual(1, _regionManager.Regions.Length);
        }
    }
}