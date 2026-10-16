namespace BlueDotBrigade.Weevil.IO
{
	using System.IO;
	using System.Text;

	[TestClass]
	public class StreamExtensionsTests
	{
		private static MemoryStream ToStream(string content)
		{
			return new MemoryStream(Encoding.UTF8.GetBytes(content));
		}

		[TestMethod]
		public void GivenOffsetZero_WhenCheckingStartOfLine_ThenReturnsTrue()
		{
			ToStream("Line 1\nLine 2").IsStartOfLine(0).Should().BeTrue();
		}

		[TestMethod]
		public void GivenOffsetAfterLineFeed_WhenCheckingStartOfLine_ThenReturnsTrue()
		{
			ToStream("Line 1\nLine 2").IsStartOfLine(7).Should().BeTrue();
		}

		[TestMethod]
		public void GivenOffsetAfterCarriageReturnLineFeed_WhenCheckingStartOfLine_ThenReturnsTrue()
		{
			ToStream("Line 1\r\nLine 2").IsStartOfLine(8).Should().BeTrue();
		}

		[TestMethod]
		public void GivenOffsetAfterCarriageReturnOnly_WhenCheckingStartOfLine_ThenReturnsTrue()
		{
			ToStream("Line 1\rLine 2").IsStartOfLine(7).Should().BeTrue();
		}

		[TestMethod]
		public void GivenOffsetBetweenCarriageReturnAndLineFeed_WhenCheckingStartOfLine_ThenReturnsFalse()
		{
			ToStream("Line 1\r\nLine 2").IsStartOfLine(7).Should().BeFalse();
		}

		[TestMethod]
		public void GivenOffsetInMiddleOfLine_WhenCheckingStartOfLine_ThenReturnsFalse()
		{
			ToStream("Line 1\nLine 2").IsStartOfLine(3).Should().BeFalse();
		}

		[TestMethod]
		public void GivenNegativeOffset_WhenCheckingStartOfLine_ThenReturnsFalse()
		{
			ToStream("Line 1\nLine 2").IsStartOfLine(-1).Should().BeFalse();
		}

		[TestMethod]
		public void GivenOffsetAtEndOfStream_WhenCheckingStartOfLine_ThenReturnsFalse()
		{
			var stream = ToStream("Line 1\n");

			stream.IsStartOfLine(stream.Length).Should().BeFalse();
		}

		[TestMethod]
		public void GivenAnyOffset_WhenCheckingStartOfLine_ThenStreamPositionIsRestored()
		{
			var stream = ToStream("Line 1\nLine 2");
			stream.Position = 2;

			stream.IsStartOfLine(7);

			stream.Position.Should().Be(2);
		}
	}
}
