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
		public void GivenInsightAttentionOpacityStyle_WhenIconAnimationIsConfigured_ThenAnimationRepeatsOnlyThreeTimes()
		{
			var statusBarViewPath = LocateStatusBarViewPath();
			var xaml = XDocument.Load(statusBarViewPath);

			var attentionAnimation = FindAttentionAnimation(
				xaml,
				styleKey: "InsightIconStyleOpacityPulse",
				targetProperty: "Opacity");

			attentionAnimation.Should().NotBeNull();
			attentionAnimation!
				.Attribute("RepeatBehavior")?
				.Value
				.Should()
				.Be("3x");
		}

		[TestMethod]
		public void GivenInsightAttentionScaleStyle_WhenIconAnimationIsConfigured_ThenAnimationScalesAndRepeatsOnlyThreeTimes()
		{
			var statusBarViewPath = LocateStatusBarViewPath();
			var xaml = XDocument.Load(statusBarViewPath);

			var scaleXAnimation = FindAttentionAnimation(
				xaml,
				styleKey: "InsightIconStyleScalePulse",
				targetProperty: "(UIElement.RenderTransform).(ScaleTransform.ScaleX)");
			var scaleYAnimation = FindAttentionAnimation(
				xaml,
				styleKey: "InsightIconStyleScalePulse",
				targetProperty: "(UIElement.RenderTransform).(ScaleTransform.ScaleY)");

			scaleXAnimation.Should().NotBeNull();
			scaleYAnimation.Should().NotBeNull();
			scaleXAnimation!
				.Attribute("RepeatBehavior")?
				.Value
				.Should()
				.Be("3x");
			scaleYAnimation!
				.Attribute("RepeatBehavior")?
				.Value
				.Should()
				.Be("3x");
		}

		private static XElement? FindAttentionAnimation(
			XDocument xaml,
			string styleKey,
			string targetProperty)
		{
			var style = xaml
				.Descendants()
				.FirstOrDefault(element =>
					element.Name.LocalName == "Style"
					&& string.Equals((string?)element.Attribute(XName.Get("Key", "http://schemas.microsoft.com/winfx/2006/xaml")), styleKey, StringComparison.Ordinal));

			style.Should().NotBeNull();

			var attentionTrigger = style!
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
					&& string.Equals((string?)element.Attribute("Storyboard.TargetProperty"), targetProperty, StringComparison.Ordinal));

			attentionAnimation.Should().NotBeNull();
			return attentionAnimation;
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
