namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;
	using BlueDotBrigade.Weevil.IO;
	using Directory = System.IO.Directory;
	using File = System.IO.File;

	[TestClass]
	public class ContentChunkTests
	{
		private string _folder = string.Empty;

		[TestInitialize]
		public void Setup()
		{
			_folder = Directory.CreateTempSubdirectory("Weevil.ContentChunkTests.").FullName;
		}

		[TestCleanup]
		public void Cleanup()
		{
			Directory.Delete(_folder, recursive: true);
		}

		[TestMethod]
		[DataRow("UTF-8")]
		[DataRow("UTF-16LE")]
		[DataRow("UTF-16BE")]
		[DataRow("UTF-32LE")]
		[DataRow("UTF-32BE")]
		public void GivenAChunkOnFile_WhenItsRecordsAreRead_ThenTheirTextIsTheSameAsInMemory(string encodingName)
		{
			Encoding encoding = encodingName switch
			{
				"UTF-8" => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
				"UTF-16LE" => new UnicodeEncoding(bigEndian: false, byteOrderMark: false),
				"UTF-16BE" => new UnicodeEncoding(bigEndian: true, byteOrderMark: false),
				"UTF-32LE" => new UTF32Encoding(bigEndian: false, byteOrderMark: false),
				_ => new UTF32Encoding(bigEndian: true, byteOrderMark: false),
			};

			// The log's lines, as the parser would find them: a single line, lines joined by each kind of line break, and invalid bytes.
			var log = new LogBuilder(encoding);
			log.Add("Before the chunk", isMultiLine: false);
			var chunkStart = log.Length;
			var records = new List<(int Offset, int Length, bool IsMultiLine)>
			{
				log.Add("A single line, with é and 😀", isMultiLine: false),
				log.Add("First line\nSecond line\rThird line\r\nFourth line", isMultiLine: true),
				log.AddInvalidBytes(),
			};
			var chunkEnd = log.Length;
			log.Add("After the chunk", isMultiLine: false);

			var logPath = Path.Combine(_folder, "Log.txt");
			File.WriteAllBytes(logPath, log.ToArray());
			var builder = new LogFileSourceBuilder(logPath, 0, log.ToArray().Length);
			builder.Append(log.ToArray());
			LogFileSource source = builder.Build();

			var inMemory = new ContentChunk(chunkStart, log.ToArray().AsSpan(chunkStart, chunkEnd - chunkStart), encoding);
			var onFile = new ContentChunk(source, chunkStart, chunkEnd - chunkStart, encoding);

			foreach ((var offset, var length, var isMultiLine) in records)
			{
				Record expected = CreateRecord(inMemory, offset - chunkStart, length, isMultiLine);
				Record actual = CreateRecord(onFile, offset - chunkStart, length, isMultiLine);

				actual.Content.Should().Be(expected.Content);
				new ContentBuffer().Read(actual).ToString().Should().Be(expected.Content);
			}
		}

		private static Record CreateRecord(ContentChunk chunk, int offset, int length, bool isMultiLine)
		{
			return new Record(1, DateTime.Now, SeverityType.Information, chunk, offset, length, isMultiLine, wasGeneratedByUi: false);
		}

		/// <summary>
		/// Writes a log's lines in an encoding, and says where each record's bytes are.
		/// </summary>
		private sealed class LogBuilder
		{
			private readonly Encoding _encoding;
			private readonly List<byte> _bytes = new List<byte>();

			public LogBuilder(Encoding encoding)
			{
				_encoding = encoding;
			}

			public int Length => _bytes.Count;

			public (int Offset, int Length, bool IsMultiLine) Add(string text, bool isMultiLine)
			{
				var offset = _bytes.Count;
				_bytes.AddRange(_encoding.GetBytes(text));
				var length = _bytes.Count - offset;
				_bytes.AddRange(_encoding.GetBytes("\n"));

				return (offset, length, isMultiLine);
			}

			/// <summary>
			/// Adds a record whose bytes are invalid in every encoding tested, and end with part of a character.
			/// </summary>
			public (int Offset, int Length, bool IsMultiLine) AddInvalidBytes()
			{
				var offset = _bytes.Count;
				_bytes.AddRange(_encoding.GetBytes("Invalid: "));
				_bytes.AddRange(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xC3 });
				var length = _bytes.Count - offset;
				_bytes.AddRange(_encoding.GetBytes("\n"));

				return (offset, length, false);
			}

			public byte[] ToArray() => _bytes.ToArray();
		}
	}
}
