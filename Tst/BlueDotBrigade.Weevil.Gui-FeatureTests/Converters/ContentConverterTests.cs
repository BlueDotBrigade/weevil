namespace BlueDotBrigade.Weevil.Gui.Converters
{
	using System;
	using System.Globalization;
	using BlueDotBrigade.Weevil.Data;

	[TestClass]
	public class ContentConverterTests
	{
		[TestMethod]
		[WorkItem(200)]
		public void Convert_LongMultiLineRecord_ReturnsTruncatedString()
		{
			var record = new Record(1, DateTime.Now, SeverityType.Debug, new Daten().AsString("LongMultiLineRecord.log"));
			record.Metadata.IsMultiLine = true;

			record.Content.Length.Should().Be(2379476);

			var actualResult = new ContentConverter()
				.Convert(record, typeof(string), true, CultureInfo.InvariantCulture)
				?.ToString();

			actualResult.Length.Should().Be(257);
		}
	}
}