namespace BlueDotBrigade.Weevil.Data
{
	using System.Text;

	[TestClass]
	public class ContentBufferTests
	{
		[TestMethod]
		public void GivenRecordsHeldAsBytes_WhenReadThroughOneBuffer_ThenEachIsItsContent()
		{
			// UTF-16, and a longer record after a shorter one, so the buffer grows.
			var encoding = new UnicodeEncoding(bigEndian: false, byteOrderMark: false);
			IRecord[] records =
			[
				R.WithContentAsBytes(1, "Short", encoding, isMultiLine: false),
				R.WithContentAsBytes(2, "A longer record\nwith a second line, and é", encoding, isMultiLine: true),
				R.WithContentAsBytes(3, "Short again", encoding, isMultiLine: false),
			];
			var buffer = new ContentBuffer();

			foreach (IRecord record in records)
			{
				buffer.Read(record).ToString().Should().Be(record.Content);
			}
		}
	}
}
