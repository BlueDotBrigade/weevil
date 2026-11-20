namespace BlueDotBrigade.Weevil.Data
{
	using BlueDotBrigade.Weevil.TestTools.Data;

	[TestClass]
	public class RecordStateTests
	{
		[TestMethod]
		public void GivenTwoViewsOfOneLog_WhenOneCommentsAndFlagsARecord_ThenBothSeeTheCommentButOnlyItSeesTheFlag()
		{
			// Comments and pins belong to the log; flags belong to a view (decisions 2 and 7 of the #990 plan).
			var annotations = new AnnotationStore();
			var first = new RecordState(annotations, new FlagSet());
			var second = new RecordState(annotations, new FlagSet());
			IRecord record = R.WithLineNumber(5);

			first.SetComment(record, "Retry starts here");
			first.SetFlagged(record, true);

			second.GetComment(record).Should().Be("Retry starts here");
			second.IsPinned(record).Should().BeTrue();
			second.IsFlagged(record).Should().BeFalse();
			first.IsFlagged(record).Should().BeTrue();
		}
	}
}
