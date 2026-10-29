namespace BlueDotBrigade.Weevil.Gui.Converters
{
	using System;
	using System.Globalization;
	using System.Text;
	using BlueDotBrigade.Weevil.Data;

	[TestClass]
	public class ContentToTruncationIconConverterTests
	{
		[TestMethod]
		public void GivenAShortRecordHeldAsBytes_WhenConverted_ThenNoTruncationIconIsShown()
		{
			// The icon showed on every row: it compared the content by reference, and each read of the bytes is a new string.
			var bytes = Encoding.UTF8.GetBytes("A short record");
			var record = new Record(1, DateTime.Now, SeverityType.Information, new ContentChunk(0, bytes, Encoding.UTF8), 0, bytes.Length, isMultiLine: false, wasGeneratedByUi: false);

			var icon = new ContentToTruncationIconConverter().Convert(record, typeof(string), null, CultureInfo.InvariantCulture);

			icon.Should().Be(string.Empty);
		}

		[TestMethod]
		public void GivenARecordTooLongForTheGrid_WhenConverted_ThenTheTruncationIconIsShown()
		{
			var record = new Record(1, DateTime.Now, SeverityType.Information, new string('x', 10_000));

			var icon = new ContentToTruncationIconConverter().Convert(record, typeof(string), null, CultureInfo.InvariantCulture);

			icon.Should().Be(" ... ");
		}
	}
}
