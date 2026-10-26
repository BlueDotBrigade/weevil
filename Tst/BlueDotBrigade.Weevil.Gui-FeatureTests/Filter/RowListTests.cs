namespace BlueDotBrigade.Weevil.Gui.Filter
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using System.Linq;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Gui.Threading;
	using BlueDotBrigade.Weevil.TestTools.Data;

	[TestClass]
	public class RowListTests
	{
		[TestMethod]
		public void GivenARowThatIsStillHeld_WhenAskedForAgain_ThenTheSameRowIsReturned()
		{
			var rows = new RowList(R.WithLineNumbers(1, 10), new AnnotationStore(), Substitute.For<ISelect>(), new UiDispatcherFake());

			var row = rows[3];

			rows[3].Should().BeSameAs(row);
			((RowViewModel)row).LineNumber.Should().Be(4);
		}

		[TestMethod]
		public void GivenRecordsWithGapsInTheirLineNumbers_WhenARowIsLookedUp_ThenItsPositionIsFound()
		{
			ImmutableArray<IRecord> results = R.WithLineNumbers(1, 20).Where(r => r.LineNumber % 2 == 0).ToImmutableArray();
			var annotations = new AnnotationStore();
			var rows = new RowList(results, annotations, Substitute.For<ISelect>(), new UiDispatcherFake());

			rows.IndexOf(new RowViewModel(R.WithLineNumber(6), annotations)).Should().Be(2);
			rows.IndexOf(new RowViewModel(R.WithLineNumber(7), annotations)).Should().Be(-1);
			rows.IndexOf(R.WithLineNumber(6)).Should().Be(-1);
			rows.IndexOfLineNumber(20).Should().Be(9);
			rows.IndexOfLineNumber(21).Should().Be(-1);
		}

		[TestMethod]
		public void GivenTwoRowsForTheSameLine_WhenCompared_ThenTheyAreEqual()
		{
			var annotations = new AnnotationStore();
			var first = new RowViewModel(R.WithLineNumber(5), annotations);
			var second = new RowViewModel(R.WithLineNumber(5), annotations);

			first.Should().Be(second);
			first.GetHashCode().Should().Be(second.GetHashCode());
			first.Should().NotBe(new RowViewModel(R.WithLineNumber(6), annotations));
		}

		[TestMethod]
		public void GivenAPinMadeElsewhere_WhenTheUiThreadRunsTheRefresh_ThenTheRowReportsIt()
		{
			var annotations = new AnnotationStore();
			var dispatcher = new DeferredUiDispatcherFake();
			var rows = new RowList(R.WithLineNumbers(1, 10), annotations, Substitute.For<ISelect>(), dispatcher);
			annotations.Changed += (_, e) => rows.QueueRefresh(e);
			RowViewModel row = rows.GetRow(4);
			var changedProperties = new List<string>();
			row.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

			annotations.SetPinned(row.LineNumber, true);

			changedProperties.Should().BeEmpty("the UI thread has not run the refresh yet");
			dispatcher.RunQueued();
			changedProperties.Should().Contain(nameof(RowViewModel.IsPinned));
			row.IsPinned.Should().BeTrue();
		}

		[TestMethod]
		public void GivenManyChanges_WhenTheyArrive_ThenTheUiThreadRefreshesTheRowsOnce()
		{
			var annotations = new AnnotationStore();
			var dispatcher = new DeferredUiDispatcherFake();
			var rows = new RowList(R.WithLineNumbers(1, 10), annotations, Substitute.For<ISelect>(), dispatcher);
			annotations.Changed += (_, e) => rows.QueueRefresh(e);

			for (var lineNumber = 1; lineNumber <= 10; lineNumber++)
			{
				annotations.SetPinned(lineNumber, true);
			}

			dispatcher.QueuedCount.Should().Be(1);
		}

		[TestMethod]
		public void GivenMoreChangesThanTheQueueHolds_WhenTheUiThreadRunsTheRefresh_ThenEveryRowStillHeldRefreshes()
		{
			var annotations = new AnnotationStore();
			var dispatcher = new DeferredUiDispatcherFake();
			var rows = new RowList(R.WithLineNumbers(1, 5_000), annotations, Substitute.For<ISelect>(), dispatcher);
			annotations.Changed += (_, e) => rows.QueueRefresh(e);
			RowViewModel row = rows.GetRow(0);
			var changedProperties = new List<string>();
			row.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

			// None of them is the row's own line.
			for (var lineNumber = 2; lineNumber <= 2_000; lineNumber++)
			{
				annotations.SetPinned(lineNumber, true);
			}

			dispatcher.RunQueued();

			changedProperties.Should().Contain(nameof(RowViewModel.IsPinned));
			GC.KeepAlive(row);
		}

		[TestMethod]
		public void GivenAChangeToManyLines_WhenTheUiThreadRunsTheRefresh_ThenEveryRowStillHeldRefreshes()
		{
			// Unpin All reports one change for many lines, not one per line.
			var annotations = new AnnotationStore();
			var dispatcher = new DeferredUiDispatcherFake();
			var rows = new RowList(R.WithLineNumbers(1, 10), annotations, Substitute.For<ISelect>(), dispatcher);
			annotations.Changed += (_, e) => rows.QueueRefresh(e);
			annotations.SetPinned(5, true);
			dispatcher.RunQueued();
			RowViewModel row = rows.GetRow(4);
			var changedProperties = new List<string>();
			row.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName);

			annotations.SetPinned(new[] { 5, 6 }, false);
			dispatcher.RunQueued();

			changedProperties.Should().Contain(nameof(RowViewModel.IsPinned));
			row.IsPinned.Should().BeFalse();
		}

		[TestMethod]
		public void GivenSelectedRecords_WhenTheSelectionChanges_ThenTheRowsStillHeldShowIt()
		{
			ImmutableArray<IRecord> results = R.WithLineNumbers(1, 10);
			var selector = Substitute.For<ISelect>();
			selector.Selected.Returns(Selected(results, 2));
			var rows = new RowList(results, new AnnotationStore(), selector, new UiDispatcherFake());
			RowViewModel second = rows.GetRow(1);
			RowViewModel third = rows.GetRow(2);
			second.IsSelected.Should().BeTrue();

			selector.Selected.Returns(Selected(results, 3));
			rows.RefreshSelection();

			second.IsSelected.Should().BeFalse();
			third.IsSelected.Should().BeTrue();
		}

		[TestMethod]
		public void GivenRowsOnScreen_WhenTheSelectionChanges_ThenOnlyTheRowsThatChangedAreReported()
		{
			// The grid tells screen readers about these rows.
			ImmutableArray<IRecord> results = R.WithLineNumbers(1, 10);
			var selector = Substitute.For<ISelect>();
			selector.Selected.Returns(Selected(results, 2, 5));
			var rows = new RowList(results, new AnnotationStore(), selector, new UiDispatcherFake());
			var heldRows = new[] { rows.GetRow(1), rows.GetRow(2), rows.GetRow(4) };
			IReadOnlyList<RowViewModel> reportedRows = null;
			rows.SelectionRefreshed += (_, changedRows) => reportedRows = changedRows;

			selector.Selected.Returns(Selected(results, 3, 5));
			rows.RefreshSelection();

			reportedRows.Select(row => row.LineNumber).Should().BeEquivalentTo(new[] { 2, 3 });
			GC.KeepAlive(heldRows);
		}

		[TestMethod]
		public void GivenSelectedRecords_WhenTheSelectedRowsAfterARowAreAskedFor_ThenTheyAreFoundInOrder()
		{
			ImmutableArray<IRecord> results = R.WithLineNumbers(1, 10);
			var selector = Substitute.For<ISelect>();
			selector.Selected.Returns(Selected(results, 3, 7, 9));
			var rows = new RowList(results, new AnnotationStore(), selector, new UiDispatcherFake());

			rows.GetSelectedPositions(afterLineNumber: -1).Should().Equal(2, 6, 8);
			rows.GetSelectedPositions(afterLineNumber: 3).Should().Equal(6, 8);
		}

		[TestMethod]
		public void GivenTheRows_WhenChangedDirectly_ThenTheChangeIsRefused()
		{
			var rows = new RowList(R.WithLineNumbers(1, 3), new AnnotationStore(), Substitute.For<ISelect>(), new UiDispatcherFake());

			Action add = () => rows.Add(new RowViewModel(R.WithLineNumber(4), new AnnotationStore()));

			rows.IsReadOnly.Should().BeTrue();
			add.Should().Throw<NotSupportedException>();
		}

		private static IDictionary<int, IRecord> Selected(ImmutableArray<IRecord> results, params int[] lineNumbers)
		{
			return lineNumbers.ToDictionary(lineNumber => lineNumber, lineNumber => results[lineNumber - 1]);
		}
	}
}
