namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Collections.Generic;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Gui.Threading;
	using BlueDotBrigade.Weevil.TestTools.Data;

	[TestClass]
	public class RowViewModelTests
	{
		[TestMethod]
		public void GivenARow_WhenItsPinIsChecked_ThenTheStorePinsTheLine()
		{
			var annotations = new AnnotationStore();
			var row = new RowViewModel(R.WithLineNumber(5), annotations);

			row.IsPinned = true;

			annotations.IsPinned(5).Should().BeTrue();
		}

		[TestMethod]
		public void GivenARow_WhenCommented_ThenTheRecordIsPinnedAndTheRowSaysSo()
		{
			var annotations = new AnnotationStore();
			var row = new RowViewModel(R.WithLineNumber(5), annotations);
			var changedProperties = new List<string>();
			row.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

			row.Comment = "Retry starts here";

			annotations.GetComment(5).Should().Be("Retry starts here");
			row.IsPinned.Should().BeTrue();
			changedProperties.Should().Contain(nameof(RowViewModel.IsPinned));
		}

		[TestMethod]
		public void GivenTheRowsOwnComment_WhenTheUiThreadRunsTheRefresh_ThenTheCommentIsNotReadBack()
		{
			var annotations = new AnnotationStore();
			var dispatcher = new DeferredUiDispatcherFake();
			var rows = new RowList(R.WithLineNumbers(1, 10), annotations, dispatcher);
			annotations.Changed += (_, e) => rows.QueueRefresh(e);
			RowViewModel row = rows.GetRow(4);
			row.Comment = "Ret";
			var changedProperties = new List<string>();
			row.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

			dispatcher.RunQueued();

			// Reading it back would replace text typed since the box sent "Ret".
			changedProperties.Should().NotContain(nameof(RowViewModel.Comment));
			GC.KeepAlive(row);
		}

		[TestMethod]
		public void GivenACommentMadeElsewhere_WhenTheUiThreadRunsTheRefresh_ThenTheRowReadsItBack()
		{
			var annotations = new AnnotationStore();
			var dispatcher = new DeferredUiDispatcherFake();
			var rows = new RowList(R.WithLineNumbers(1, 10), annotations, dispatcher);
			annotations.Changed += (_, e) => rows.QueueRefresh(e);
			RowViewModel row = rows.GetRow(4);
			var changedProperties = new List<string>();
			row.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

			annotations.SetComment(row.LineNumber, "Pasted comment");
			dispatcher.RunQueued();

			changedProperties.Should().Contain(nameof(RowViewModel.Comment));
			row.Comment.Should().Be("Pasted comment");
		}

		[TestMethod]
		public void GivenARow_WhenReadByTheGrid_ThenItShowsItsRecord()
		{
			IRecord record = R.WithLineNumber(5);

			var row = new RowViewModel(record, new AnnotationStore());

			row.Severity.Should().Be(record.Severity);
			RowViewModel.ToRecord(row).Should().BeSameAs(record);
			RowViewModel.ToRecord(record).Should().BeSameAs(record);
		}
	}
}
