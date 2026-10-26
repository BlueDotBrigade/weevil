namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Generic;

	[TestClass]
	public class AnnotationStoreTests
	{
		[TestMethod]
		public void GivenLineWithoutComment_WhenCommentAdded_ThenLineIsPinned()
		{
			var store = new AnnotationStore(10);

			store.SetComment(5, "Retry starts here");

			store.GetComment(5).Should().Be("Retry starts here");
			store.HasComment(5).Should().BeTrue();
			store.IsPinned(5).Should().BeTrue();
		}

		[TestMethod]
		public void GivenCommentedLine_WhenCommentRemoved_ThenLineIsUnpinned()
		{
			var store = new AnnotationStore(10);
			store.SetComment(5, "Retry starts here");

			store.SetComment(5, string.Empty);

			store.GetComment(5).Should().BeEmpty();
			store.HasComment(5).Should().BeFalse();
			store.IsPinned(5).Should().BeFalse();
		}

		[TestMethod]
		public void GivenCommentedLineThatWasUnpinned_WhenCommentChanged_ThenLineStaysUnpinned()
		{
			var store = new AnnotationStore(10);
			store.SetComment(5, "First");
			store.SetPinned(5, false);

			store.SetComment(5, "Second");

			store.IsPinned(5).Should().BeFalse();
		}

		[TestMethod]
		public void GivenWhiteSpaceComment_WhenSet_ThenItIsKeptButIsNotAComment()
		{
			var store = new AnnotationStore(10);

			store.SetComment(5, "  ");

			store.GetComment(5).Should().Be("  ");
			store.HasComment(5).Should().BeFalse();
			store.IsPinned(5).Should().BeFalse();
		}

		[TestMethod]
		public void GivenSavedCommentThatIsNotPinned_WhenLoaded_ThenItStaysUnpinned()
		{
			var store = new AnnotationStore(10);

			store.Load(5, "Saved comment", isPinned: false);

			store.GetComment(5).Should().Be("Saved comment");
			store.IsPinned(5).Should().BeFalse();
		}

		[TestMethod]
		public void GivenNegativeLine_WhenCommented_ThenItIsRejectedAndNoCommentIsKept()
		{
			var store = new AnnotationStore(10);

			Action setComment = () => store.SetComment(-1, "Retry starts here");

			setComment.Should().Throw<ArgumentOutOfRangeException>();
			store.GetComment(-1).Should().BeEmpty();
		}

		[TestMethod]
		public void GivenLineBeyondTheExpectedCount_WhenAnnotated_ThenTheStoreGrows()
		{
			var store = new AnnotationStore(10);

			store.SetPinned(1_000, true);

			store.IsPinned(1_000).Should().BeTrue();
			store.IsPinned(999).Should().BeFalse();
			store.IsPinned(10_000).Should().BeFalse();
		}

		[TestMethod]
		public void GivenACommentAndAPin_WhenEachIsChanged_ThenChangedReportsTheLineAndWhoChangedIt()
		{
			var store = new AnnotationStore(10);
			var changes = new List<AnnotationChangedEventArgs>();
			store.Changed += (_, e) => changes.Add(e);
			var row = new object();

			store.SetComment(5, "Retry starts here", row);
			store.SetPinned(6, true);

			changes.Should().HaveCount(2);
			changes[0].LineNumber.Should().Be(5);
			changes[0].Source.Should().BeSameAs(row);
			changes[1].LineNumber.Should().Be(6);
			changes[1].Source.Should().BeNull();
		}

		[TestMethod]
		public void GivenACommentAndAPin_WhenSetToTheSameValues_ThenChangedIsNotRaised()
		{
			var store = new AnnotationStore(10);
			store.SetComment(5, "Retry starts here");
			store.SetPinned(6, true);
			var changes = 0;
			store.Changed += (_, _) => changes++;

			store.SetComment(5, "Retry starts here");
			store.SetPinned(6, true);
			store.SetPinned(7, false);

			changes.Should().Be(0);
		}

		[TestMethod]
		public void GivenTheSidecarIsLoaded_WhenALineIsRestored_ThenChangedIsNotRaised()
		{
			var store = new AnnotationStore(10);
			var changes = 0;
			store.Changed += (_, _) => changes++;

			store.Load(5, "Retry starts here", isPinned: true);

			changes.Should().Be(0);
		}

		[TestMethod]
		public void GivenAListener_WhenChangedIsRaised_ThenTheNewValueIsAlreadyReadable()
		{
			var store = new AnnotationStore(10);
			var pinnedWhenRaised = false;
			store.Changed += (_, e) => pinnedWhenRaised = store.IsPinned(e.LineNumber);

			store.SetComment(5, "Retry starts here");

			pinnedWhenRaised.Should().BeTrue();
		}

		[TestMethod]
		public void GivenPinnedAndCommentedLines_WhenUnpinnedTogether_ThenChangedIsRaisedOnceAndTheCommentsStay()
		{
			// Unpin All: the grid refreshes once instead of once per line (decision 4 of the #990 plan).
			var store = new AnnotationStore(10);
			store.SetPinned(3, true);
			store.SetComment(5, "Retry starts here");
			var changes = new List<AnnotationChangedEventArgs>();
			store.Changed += (_, e) => changes.Add(e);

			store.SetPinned(new[] { 3, 4, 5 }, false);

			changes.Should().ContainSingle().Which.LineNumber.Should().Be(AnnotationChangedEventArgs.ManyLines);
			store.IsPinned(3).Should().BeFalse();
			store.IsPinned(5).Should().BeFalse();
			store.GetComment(5).Should().Be("Retry starts here");
		}

		[TestMethod]
		public void GivenCommentedLines_WhenTheirCommentsAreRemovedTogether_ThenTheyAreUnpinnedAndChangedIsRaisedOnce()
		{
			var store = new AnnotationStore(10);
			store.SetComment(3, "Connected");
			store.SetComment(5, "Retry starts here");
			store.SetComment(7, "Not removed");
			var changes = new List<AnnotationChangedEventArgs>();
			store.Changed += (_, e) => changes.Add(e);

			store.RemoveComments(new[] { 3, 4, 5 });

			changes.Should().ContainSingle().Which.LineNumber.Should().Be(AnnotationChangedEventArgs.ManyLines);
			store.GetComment(3).Should().BeEmpty();
			store.IsPinned(3).Should().BeFalse();
			store.GetComment(5).Should().BeEmpty();
			store.IsPinned(7).Should().BeTrue();
		}

		[TestMethod]
		public void GivenNegativeLine_WhenUnpinned_ThenItIsRejected()
		{
			var store = new AnnotationStore(10);

			Action unpin = () => store.SetPinned(-1, false);

			unpin.Should().Throw<ArgumentOutOfRangeException>();
		}
	}
}
