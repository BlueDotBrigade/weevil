namespace BlueDotBrigade.Weevil.Gui.Converters
{
	using System;
	using System.Globalization;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Gui.Filter;

	[TestClass]
	public class ContentConverterTests
	{
		[TestMethod]
		[WorkItem(200)]
		public void Convert_LongMultiLineRecord_ReturnsTruncatedString()
		{
			var record = new Record(1, DateTime.Now, SeverityType.Debug, new Daten().AsString(), new Metadata(), isMultiLine: true, wasGeneratedByUi: false);

			record.Content.Length.Should().Be(2379476);

			var actualResult = new ContentConverter()
				.Convert(record, typeof(string), true, CultureInfo.InvariantCulture)
				?.ToString();

			actualResult.Length.Should().Be(257);
		}

		[TestMethod]
		public void GivenAGridRow_WhenConverted_ThenTheResultIsTheSameAsForItsRecord()
		{
			var record = new Record(1, DateTime.Now, SeverityType.Debug, new string('x', 1_000), new Metadata());
			var row = new RowViewModel(record, new AnnotationStore());
			var converter = new ContentConverter();

			var fromRow = converter.Convert(row, typeof(string), null, CultureInfo.InvariantCulture);

			fromRow.Should().Be(converter.Convert(record, typeof(string), null, CultureInfo.InvariantCulture));
		}
	}
}