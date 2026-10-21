namespace BlueDotBrigade.Weevil.Data
{
	using System;

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
	}
}
