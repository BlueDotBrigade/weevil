namespace BlueDotBrigade.Weevil.Gui.IO
{
	using System;
	using System.Collections.Generic;
	using System.Windows.Threading;
	using BlueDotBrigade.Weevil.Gui.Threading;
	using BlueDotBrigade.Weevil.IO;

	// Regression: Issue #996 - an analyzer running on a background thread shows its prompts on the UI thread.
	[TestClass]
	public class UiThreadUserDialogTests
	{
		[TestMethod]
		public void GivenAnalyzerPrompt_WhenShown_ThenRunsThroughUiDispatcher()
		{
			var events = new List<string>();
			var uiDispatcher = new RecordingUiDispatcher(events);

			var dialog = Substitute.For<IUserDialog>();
			dialog.ShowUserPrompt("Title", "Prompt", "Default").Returns(_ =>
			{
				events.Add("prompt");
				return "Answer";
			});

			var userDialog = new UiThreadUserDialog(
				dialog,
				uiDispatcher,
				promptOpening: () => events.Add("opening"),
				promptClosed: () => events.Add("closed"));

			userDialog.ShowUserPrompt("Title", "Prompt", "Default").Should().Be("Answer");

			events.Should().Equal("dispatcher", "opening", "prompt", "closed");
		}

		[TestMethod]
		public void GivenExpressionPrompt_WhenAccepted_ThenReturnsTheUsersAnswer()
		{
			var dialog = Substitute.For<IUserDialog>();
			dialog.TryGetExpressions("Default", "10", out Arg.Any<string>()).Returns(x =>
			{
				x[2] = "Id=(?<Id>\\d+)";
				return true;
			});

			var userDialog = new UiThreadUserDialog(dialog, new UiDispatcherFake(), () => { }, () => { });

			userDialog.TryGetExpressions("Default", "10", out var regularExpression).Should().BeTrue();
			regularExpression.Should().Be("Id=(?<Id>\\d+)");
		}

		[TestMethod]
		public void GivenPromptThatFails_WhenShown_ThenPromptIsStillReportedClosed()
		{
			var wasClosed = false;

			var dialog = Substitute.For<IUserDialog>();
			dialog.ShowUserPrompt(Arg.Any<string>(), Arg.Any<string>()).Returns<string>(_ => throw new InvalidOperationException());

			var userDialog = new UiThreadUserDialog(dialog, new UiDispatcherFake(), () => { }, () => wasClosed = true);

			Action showPrompt = () => userDialog.ShowUserPrompt("Title", "Prompt");

			showPrompt.Should().Throw<InvalidOperationException>();
			wasClosed.Should().BeTrue();
		}

		private sealed class RecordingUiDispatcher : IUiDispatcher
		{
			private readonly List<string> _events;

			public RecordingUiDispatcher(List<string> events)
			{
				_events = events;
			}

			public void Invoke(Action callback)
			{
				_events.Add("dispatcher");
				callback();
			}

			public void Invoke(Action callback, DispatcherPriority priority)
			{
				Invoke(callback);
			}

			public void BeginInvoke(Action callback, DispatcherPriority priority)
			{
				Invoke(callback);
			}
		}
	}
}
