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
        public void MarkRegionStart_ValidLineNumber_StartLineNumberSet()
        {
            // Arrange
            int startLineNumber = 800;

            // Act
            _regionManager.MarkRegionStart(startLineNumber);

            // Assert
            Assert.AreEqual(startLineNumber, _regionManager.CurrentRegionStartLineNumber);
        }

        [TestMethod]
        public void MarkRegionEnd_AfterStartRegion_RegionAdded()
        {
            // Arrange
            int startLineNumber = 800;
            int endLineNumber = 900;
            _regionManager.MarkRegionStart(startLineNumber);

            // Act
            _regionManager.MarkRegionEnd(endLineNumber);

            // Assert
            var regions = _regionManager.GetRegionsOfInterest();
            Assert.AreEqual(1, regions.Count);
            Assert.AreEqual(startLineNumber, regions[0].StartLineNumber);
            Assert.AreEqual(endLineNumber, regions[0].EndLineNumber);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void MarkRegionEnd_WithoutStart_ThrowsInvalidOperationException()
        {
            // Arrange
            int endLineNumber = 900;

            // Act
            _regionManager.MarkRegionEnd(endLineNumber);

            // Assert handled by ExpectedException
        }

        [TestMethod]
        public void GetRegionsOfInterest_AfterMultipleRegions_ReturnsAllRegions()
        {
            // Arrange
            _regionManager.MarkRegionStart(800);
            _regionManager.MarkRegionEnd(900);

            _regionManager.MarkRegionStart(950);
            _regionManager.MarkRegionEnd(1000);

            // Act
            var regions = _regionManager.GetRegionsOfInterest();

            // Assert
            Assert.AreEqual(2, regions.Count);
            Assert.AreEqual(800, regions[0].StartLineNumber);
            Assert.AreEqual(900, regions[0].EndLineNumber);
            Assert.AreEqual(950, regions[1].StartLineNumber);
            Assert.AreEqual(1000, regions[1].EndLineNumber);
        }

        [TestMethod]
        public void ClearRegions_AfterAddingRegions_RegionsCleared()
        {
            // Arrange
            _regionManager.MarkRegionStart(800);
            _regionManager.MarkRegionEnd(900);

            // Act
            _regionManager.ClearRegions();

            // Assert
            var regions = _regionManager.GetRegionsOfInterest();
            Assert.AreEqual(0, regions.Count);
        }
    }
}