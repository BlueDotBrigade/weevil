namespace BlueDotBrigade.Weevil.Configuration.Sidecar.v2
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	[TestClass]
	public class SidecarLoaderTests
	{
		[TestMethod]
		public void Load_SchemaWithoutRegions_ReturnsEmptyBookmarks()
		{
			// assert
			var metadata = new Daten().AsString();
			var metadataPath = new Daten().AsFilePath();
			var sidecarLoader = new SidecarLoader(metadataPath);

			// act
			sidecarLoader.TryLoad(out var sidecarData);

			// arrange
			metadata.Should().NotContain("Regions");
			sidecarData.CommonData.Regions.Should().BeEmpty();
		}

		[TestMethod]
		public void Load_SchemaWithRegions_ReturnsEmptyBookmarks()
		{
			// assert
			var metadata = new Daten().AsString();
			var metadataPath = new Daten().AsFilePath();
			var sidecarLoader = new SidecarLoader(metadataPath);

			// act
			sidecarLoader.TryLoad(out var sidecarData);

			// arrange
			sidecarData.CommonData.Regions[0].Name.Should().Be("Region Of Interest");
			sidecarData.CommonData.Regions[0].Minimum.LineNumber.Should().Be(12);
			sidecarData.CommonData.Regions[0].Maximum.LineNumber.Should().Be(34);
		}
	}
}