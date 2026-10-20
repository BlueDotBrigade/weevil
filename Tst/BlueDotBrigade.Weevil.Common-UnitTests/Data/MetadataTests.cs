namespace BlueDotBrigade.Weevil.Data
{
	using System.Collections.Generic;

	[TestClass]
	public class MetadataTests
	{
		[TestMethod]
		public void GivenRecordWithoutComment_WhenCommentAdded_ThenPinnedAndEveryChangeIsRaised()
		{
			var metadata = new Metadata();
			var changes = new List<string>();
			metadata.PropertyChanged += (sender, e) => changes.Add(e.PropertyName);

			metadata.Comment = "Retry starts here";

			metadata.IsPinned.Should().BeTrue();
			changes.Should().Equal(nameof(Metadata.IsPinned), nameof(Metadata.Comment), nameof(Metadata.HasComment));
		}

		[TestMethod]
		public void GivenValuesSetBeforeAttaching_WhenAttached_ThenTheStoresHaveThem()
		{
			var metadata = new Metadata { Comment = "Retry starts here", IsFlagged = true };
			metadata.IsPinned = false;
			var view = new ViewAnnotations();

			metadata.Attach(view, lineNumber: 7);

			view.Annotations.GetComment(7).Should().Be("Retry starts here");
			view.Annotations.IsPinned(7).Should().BeFalse();
			view.Flags.IsFlagged(7).Should().BeTrue();
		}

		[TestMethod]
		public void GivenTwoRecordsAttachedToTheSameLine_WhenOneIsCommented_ThenTheOtherSeesIt()
		{
			// Clear and Reload keep the log's annotations, so a record created again sees them (#1013).
			var view = new ViewAnnotations();
			var before = new Metadata();
			var after = new Metadata();
			before.Attach(view, lineNumber: 7);
			after.Attach(view, lineNumber: 7);

			before.Comment = "Retry starts here";
			before.IsFlagged = true;

			after.Comment.Should().Be("Retry starts here");
			after.IsPinned.Should().BeTrue();
			after.IsFlagged.Should().BeTrue();
		}

		[TestMethod]
		public void GivenAttachedRecord_WhenAttachedAgain_ThenItsValuesAreNotCopiedOverTheStore()
		{
			var view = new ViewAnnotations();
			var metadata = new Metadata();
			metadata.Attach(view, lineNumber: 7);
			metadata.Comment = "Retry starts here";
			view.Annotations.SetPinned(7, false);

			metadata.Attach(view, lineNumber: 7);

			view.Annotations.IsPinned(7).Should().BeFalse();
		}
	}
}
