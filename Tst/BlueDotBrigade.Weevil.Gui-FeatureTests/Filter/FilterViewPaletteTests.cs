namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Xml.Linq;

	[TestClass]
	public class FilterViewPaletteTests
	{
		[TestMethod]
		public void GivenApplicationResources_WhenDarkPaletteOverridesAreConfigured_ThenNearBlackSurfacesRemainCentralized()
		{
			var applicationStyles = XDocument.Load(LocateRepositoryFilePath("Src", "BlueDotBrigade.Weevil.Gui", "Themes", "ApplicationStyles.xaml"));
			var app = XDocument.Load(LocateRepositoryFilePath("Src", "BlueDotBrigade.Weevil.Gui", "App.xaml"));

			FindResource(applicationStyles, "Color", "SurfaceBackgroundColor")
				.Attribute("Value")?
				.Value
				.Should()
				.Be("#0D1117");
			FindResource(applicationStyles, "Color", "SurfaceCardColor")
				.Attribute("Value")?
				.Value
				.Should()
				.Be("#161B22");
			FindResource(applicationStyles, "Color", "SecondaryBackgroundColor")
				.Attribute("Value")?
				.Value
				.Should()
				.Be("#21262D");
			FindResource(applicationStyles, "Color", "RowHighlightedColor")
				.Attribute("Value")?
				.Value
				.Should()
				.Be("#900A22");

			FindResource(app, "SolidColorBrush", "MaterialDesignPaper")
				.Attribute("Color")?
				.Value
				.Should()
				.Be("{StaticResource SurfaceBackgroundColor}");
			FindResource(app, "SolidColorBrush", "MaterialDesignCardBackground")
				.Attribute("Color")?
				.Value
				.Should()
				.Be("{StaticResource SurfaceCardColor}");
			FindResource(app, "SolidColorBrush", "MaterialDesignDivider")
				.Attribute("Color")?
				.Value
				.Should()
				.Be("{StaticResource SubtleOutlineColor}");
		}

		[TestMethod]
		public void GivenFilterView_WhenResultsListRowsAreStyled_ThenAlternatingRowsAndSelectionUseDistinctPaletteBrushes()
		{
			var filterView = XDocument.Load(LocateRepositoryFilePath("Src", "BlueDotBrigade.Weevil.Gui", "Filter", "FilterView.xaml"));

			FindSetter(
					filterView,
					triggerProperty: "ItemsControl.AlternationIndex",
					triggerValue: "0",
					setterProperty: "Background")
				.Attribute("Value")?
				.Value
				.Should()
				.Be("{Binding Severity, Converter={StaticResource SeverityBackgroundConverter}, FallbackValue={StaticResource RowBackgroundBrush}, Mode=OneWay}");

			FindSetter(
					filterView,
					triggerProperty: "ItemsControl.AlternationIndex",
					triggerValue: "1",
					setterProperty: "Background")
				.Attribute("Value")?
				.Value
				.Should()
				.Be("{Binding Severity, Converter={StaticResource SeverityBackgroundConverter}, FallbackValue={StaticResource AlternatingRowBackgroundBrush}, Mode=OneWay}");

			FindSetter(
					filterView,
					triggerProperty: "IsSelected",
					triggerValue: "True",
					setterProperty: "Background")
				.Attribute("Value")?
				.Value
				.Should()
				.Be("{StaticResource RowHighlightedBrush}");

			filterView
				.Descendants()
				.First(element =>
					element.Name.LocalName == "Border"
					&& string.Equals((string?)element.Attribute(XName.Get("Name", "http://schemas.microsoft.com/winfx/2006/xaml")), "BookmarkTag", StringComparison.Ordinal))
				.Attribute("Background")?
				.Value
				.Should()
				.Be("{StaticResource SecondaryBackgroundBrush}");
		}

		private static XElement FindResource(XDocument xaml, string localName, string key)
		{
			return xaml
				.Descendants()
				.First(element =>
					element.Name.LocalName == localName
					&& string.Equals((string?)element.Attribute(XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml")), key, StringComparison.Ordinal));
		}

		private static XElement FindSetter(
			XDocument xaml,
			string triggerProperty,
			string triggerValue,
			string setterProperty)
		{
			return xaml
				.Descendants()
				.First(element =>
					element.Name.LocalName == "Trigger"
					&& string.Equals((string?)element.Attribute("Property"), triggerProperty, StringComparison.Ordinal)
					&& string.Equals((string?)element.Attribute("Value"), triggerValue, StringComparison.Ordinal))
				.Elements()
				.First(element =>
					element.Name.LocalName == "Setter"
					&& string.Equals((string?)element.Attribute("Property"), setterProperty, StringComparison.Ordinal));
		}

		private static string LocateRepositoryFilePath(params string[] pathParts)
		{
			var current = new DirectoryInfo(AppContext.BaseDirectory);

			while (current is not null && !File.Exists(Path.Combine(current.FullName, "Weevil-v2.sln")))
			{
				current = current.Parent;
			}

			current.Should().NotBeNull("tests should run from within the repository tree");

			return Path.Combine(new[] { current!.FullName }.Concat(pathParts).ToArray());
		}
	}
}
