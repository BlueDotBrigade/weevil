namespace BlueDotBrigade.Weevil.Data
{
	using System.Linq;
	using System.Threading.Tasks;

	[TestClass]
	public class FlagSetTests
	{
		[TestMethod]
		public void GivenFlaggedLine_WhenUnflagged_ThenOnlyThatLineChanges()
		{
			var flags = new FlagSet(100);
			flags.SetFlagged(63, true);
			flags.SetFlagged(64, true);

			flags.SetFlagged(63, false);

			flags.IsFlagged(63).Should().BeFalse();
			flags.IsFlagged(64).Should().BeTrue();
		}

		[TestMethod]
		public void GivenLineBeyondTheExpectedCount_WhenFlagged_ThenTheSetGrows()
		{
			var flags = new FlagSet(10);

			flags.SetFlagged(1_000, true);

			flags.IsFlagged(1_000).Should().BeTrue();
			flags.IsFlagged(10_000).Should().BeFalse();
			flags.IsFlagged(-1).Should().BeFalse();
		}

		[TestMethod]
		public void GivenFlaggedLines_WhenCleared_ThenNoLineIsFlagged()
		{
			var flags = new FlagSet(10);
			flags.SetFlagged(5, true);
			flags.SetFlagged(1_000, true);

			flags.Clear();

			flags.IsFlagged(5).Should().BeFalse();
			flags.IsFlagged(1_000).Should().BeFalse();
		}

		[TestMethod]
		public void GivenManyThreads_WhenEachFlagsItsOwnLines_ThenEveryLineIsFlagged()
		{
			var flags = new FlagSet(0);

			Parallel.For(0, 100_000, lineNumber => flags.SetFlagged(lineNumber, true));

			Enumerable.Range(0, 100_000).All(flags.IsFlagged).Should().BeTrue();
		}
	}
}
