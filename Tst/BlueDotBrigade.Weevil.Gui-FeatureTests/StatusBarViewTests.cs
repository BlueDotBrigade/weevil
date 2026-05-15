namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Xml.Linq;
	using BlueDotBrigade.Weevil.Gui.Analysis;
	using BlueDotBrigade.Weevil.Gui.IO;
	using BlueDotBrigade.Weevil.Gui.Threading;

	[TestClass]
	public class StatusBarViewTests
	{
		[TestMethod]
		// Regression #844: Animation should run at half the original speed (400ms per direction).
		public void GivenInsightAttentionOpacityStyle_WhenIconAnimationIsConfigured_ThenAnimationDurationIsHalfSpeed()
		{
			var statusBarViewPath = LocateStatusBarViewPath();
			var xaml = XDocument.Load(statusBarViewPath);

			var attentionAnimation = FindAttentionAnimation(
				xaml,
				styleKey: "InsightIconStyleOpacityPulse",
				targetProperty: "Opacity");

			attentionAnimation.Should().NotBeNull();
			attentionAnimation!
				.Attribute("Duration")?
				.Value
				.Should()
				.Be("0:0:0.4");
		}

		[TestMethod]
		// Regression #844: Animation should continue for five seconds rather than stopping after three iterations.
		public void GivenInsightAttentionOpacityStyle_WhenIconAnimationIsConfigured_ThenAnimationRunsForFiveSeconds()
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
				.Be("0:0:5");
		}

		[TestMethod]
		// Regression #844: Scale animation should run at half the original speed (400ms per direction).
		public void GivenInsightAttentionScaleStyle_WhenIconAnimationIsConfigured_ThenAnimationDurationIsHalfSpeed()
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
				.Attribute("Duration")?
				.Value
				.Should()
				.Be("0:0:0.4");
			scaleYAnimation!
				.Attribute("Duration")?
				.Value
				.Should()
				.Be("0:0:0.4");
		}

		[TestMethod]
		// Regression #844: Scale animation should continue for five seconds rather than stopping after three iterations.
		public void GivenInsightAttentionScaleStyle_WhenIconAnimationIsConfigured_ThenAnimationRunsForFiveSeconds()
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
				.Be("0:0:5");
			scaleYAnimation!
				.Attribute("RepeatBehavior")?
				.Value
				.Should()
				.Be("0:0:5");
		}

		[TestMethod]
		// Regression #844: Insight attention animation must re-trigger when a second log file is opened.
		// The animation is driven by a WPF MultiDataTrigger on HasInsightNeedingAttention. If this property
		// remains True across file transitions, the trigger never exits and re-enters, so the animation
		// would not play for the new file. Resetting InsightDetails on file open ensures the trigger
		// transitions through False → True, allowing the animation to fire again.
		public void GivenInsightRequiresAttention_WhenSecondFileIsOpened_ThenInsightDetailsIsReset()
		{
			var uiDispatcher = new UiDispatcherFake();
			var bulletinMediator = new BulletinMediator();
			var statusBar = new StatusBarViewModel(uiDispatcher, bulletinMediator);

			// Simulate first file triggering insight attention.
			bulletinMediator.Post(new InsightChangedBulletin
			{
				HasInsight = true,
				InsightNeedingAttention = 1,
			});

			statusBar.InsightDetails.HasInsightNeedingAttention.Should().BeTrue();

			// Open a second file — this should reset insight state (False),
			// so that the subsequent insight bulletin re-triggers the animation (True).
			bulletinMediator.Post(new SourceFileOpenedBulletin
			{
				SourceFilePath = @"C:\Logs\second.log",
				TotalRecordCount = 10,
				SourceFileLoadingPeriod = TimeSpan.FromSeconds(1),
				Context = ContextDictionary.Empty,
			});

			statusBar.InsightDetails.HasInsightNeedingAttention.Should().BeFalse(
				"insight state must be reset when a new file is opened so the attention animation can re-trigger");

			// Simulate second file triggering insight attention — the transition
			// False → True is what causes the MultiDataTrigger to re-enter and fire the animation.
			bulletinMediator.Post(new InsightChangedBulletin
			{
				HasInsight = true,
				InsightNeedingAttention = 1,
			});

			statusBar.InsightDetails.HasInsightNeedingAttention.Should().BeTrue(
				"the insight bulletin posted after file open must set HasInsightNeedingAttention back to true");
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
