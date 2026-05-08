namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Xml.Linq;

	[TestClass]
	public class StatusBarViewTests
	{
		[TestMethod]
		// Regression: Insight icon should not animate indefinitely when attention is required.
		public void GivenInsightAttentionTrigger_WhenIconAnimationIsConfigured_ThenAnimationRepeatsOnlyThreeTimes()
		{
			var statusBarViewPath = LocateStatusBarViewPath();
			var xaml = XDocument.Load(statusBarViewPath);

			var attentionTrigger = xaml
				.Descendants()
				.FirstOrDefault(element =>
					element.Name.LocalName == "MultiDataTrigger"
					&& element
						.Descendants()
						.Any(condition =>
							condition.Name.LocalName == "Condition"
							&& string.Equals((string?)condition.Attribute("Binding"), "{Binding InsightDetails.HasInsightNeedingAttention}", StringComparison.Ordinal)
							&& string.Equals((string?)condition.Attribute("Value"), "True", StringComparison.Ordinal)));

			attentionTrigger.Should().NotBeNull();

			var attentionAnimation = attentionTrigger!
				.Descendants()
				.FirstOrDefault(element =>
					element.Name.LocalName == "DoubleAnimation"
					&& string.Equals((string?)element.Attribute("Storyboard.TargetProperty"), "Opacity", StringComparison.Ordinal));

			attentionAnimation.Should().NotBeNull();
			attentionAnimation!
				.Attribute("RepeatBehavior")?
				.Value
				.Should()
				.Be("3x");
		}

		private static string LocateStatusBarViewPath()
		{
			var current = new DirectoryInfo(AppContext.BaseDirectory);

			while (current is not null && !File.Exists(Path.Combine(current.FullName, "Weevil-v2.sln")))
			{
				current = current.Parent;
			}

			current.Should().NotBeNull("tests should run from within the repository tree");

			return Path.Combine(
				current!.FullName,
				"Src",
				"BlueDotBrigade.Weevil.Gui",
				"StatusBarView.xaml");
		}
	}
}
