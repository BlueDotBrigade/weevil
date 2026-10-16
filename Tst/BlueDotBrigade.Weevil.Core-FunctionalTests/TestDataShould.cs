namespace BlueDotBrigade.Weevil
{
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Xml.Linq;

	/// <summary>
	/// Verifies that test data is checked out the same way on every machine (issue #961).
	/// </summary>
	[TestClass]
	public class TestDataShould
	{
		private static readonly XNamespace SidecarNamespace =
			"http://schemas.datacontract.org/2004/07/BlueDotBrigade.Weevil.Configuration.Sidecar.v2";

		[TestMethod]
		public void GivenGlobalDefaultSidecar_WhenReadingTableOfContents_ThenEachByteOffsetIsTheStartOfItsLine()
		{
			var logFilePath = new Daten().AsFilePath(From.GlobalDefault);
			var lineStarts = GetLineStarts(File.ReadAllBytes(logFilePath));

			var sections = XDocument
				.Load($"{logFilePath}.xml")
				.Descendants(SidecarNamespace + "Section")
				.Select(section => section.Element(SidecarNamespace + "RelatesTo"))
				.ToList();

			sections.Should().NotBeEmpty();

			foreach (XElement relatesTo in sections)
			{
				var lineNumber = (int)relatesTo.Element(SidecarNamespace + "LineNumber");
				var byteOffset = (long)relatesTo.Element(SidecarNamespace + "ByteOffset");

				byteOffset.Should().Be(lineStarts[lineNumber - 1], $"line {lineNumber} starts at its saved byte offset");
			}
		}

		private static List<long> GetLineStarts(byte[] content)
		{
			var lineStarts = new List<long> { 0 };

			for (var i = 0; i < content.Length; i++)
			{
				if (content[i] == '\n')
				{
					lineStarts.Add(i + 1);
				}
			}

			return lineStarts;
		}
	}
}
