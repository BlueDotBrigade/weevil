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
			var record = new Record(1, DateTime.Now, SeverityType.Debug, new Daten().AsString());
			record.Metadata.IsMultiLine = true;

			Assert.AreEqual(2379476, record.Content.Length);

			var actualResult = new ContentConverter()
				.Convert(record, typeof(string), true, CultureInfo.InvariantCulture)
				?.ToString();

			Assert.AreEqual(257, actualResult.Length);
		}
	}
}