namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;
	using BlueDotBrigade.Weevil.Data;

	/// <summary>
	/// The format of a saved index, and what one holds besides its line index: what parsed the log, and whether the record table follows (#1023).
	/// </summary>
	/// <remarks>
	/// After the signature and the format version come <see cref="IndexSection"/>s, each with its own checksum, in this order:
	/// - the header (<see cref="IndexHeader"/>);
	/// - the line index's checkpoints;
	/// - what parsed the log (<see cref="SavedParse"/>), only after a full parse;
	/// - the record table's chunk directory, strings and rows (<see cref="RecordTable"/>), only with the parse that made them;
	/// - an end marker, at the end of the file, so a truncated or extended file is rejected.
	/// The sections stream, so the file can be larger than 2 GB, and is read with no limit on its size.
	/// An instance is immutable: it says what the file held when it was read.
	/// </remarks>
	internal sealed class IndexFile
	{
		/// <summary>
		/// What a log with no saved index has: no parse, and no table.
		/// </summary>
		public static readonly IndexFile Empty = new IndexFile(parse: null, tableOffset: 0);

		// Version 2 adds sections, each with its own checksum, and what parsed the log; version 3 adds the record table (#1023). An index of another version is rebuilt.
		private const int FormatVersion = 3;

		private static readonly byte[] Signature = "WIDX"u8.ToArray();

		private IndexFile(SavedParse parse, long tableOffset)
		{
			this.Parse = parse;
			this.TableOffset = tableOffset;
		}

		/// <summary>
		/// What parsed the log, and the checksums of the bytes it read, when the file has them: otherwise <see langword="null"/>.
		/// </summary>
		/// <remarks>
		/// It describes the log as it was parsed, so it may cover less of the log than the line index, or more, for a log written to while it was parsed after being indexed.
		/// </remarks>
		public SavedParse Parse { get; }

		/// <summary>
		/// The file holds the record table that <see cref="Parse"/> made.
		/// </summary>
		public bool HasTable => this.TableOffset > 0;

		/// <summary>
		/// Where the record table's sections start in the file, when it has them: otherwise 0.
		/// </summary>
		/// <remarks>
		/// They are checked only when they are read, by <see cref="SavedTableReader"/>, so a damaged table costs only the table.
		/// </remarks>
		public long TableOffset { get; }

		/// <summary>
		/// Writes the line index, with what parsed the log and the record table it made, when given.
		/// </summary>
		/// <param name="destination">A stream that can seek, such as a file.</param>
		/// <param name="parse">What parsed the log, or <see langword="null"/>.</param>
		/// <param name="table">The record table that <paramref name="parse"/> made, or <see langword="null"/>.</param>
		public static void Write(Stream destination, LineIndex lines, SavedParse parse = null, RecordTable table = null)
		{
			ArgumentNullException.ThrowIfNull(lines);

			if (table != null && parse == null)
			{
				throw new InvalidOperationException("A record table is saved only with the parse that made it.");
			}

			using (var writer = new BinaryWriter(destination, Encoding.UTF8, leaveOpen: true))
			{
				writer.Write(Signature);
				writer.Write(FormatVersion);
			}

			IndexHeader header = lines.ToHeader() with { HasParse = parse != null, HasTable = table != null };
			IndexSection.Write(destination, IndexSectionKind.Header, header.Write);

			IndexSection.Write(destination, IndexSectionKind.Checkpoints, writer =>
			{
				foreach (var checkpoint in lines.Checkpoints)
				{
					writer.Write(checkpoint);
				}
			});

			if (parse != null)
			{
				IndexSection.Write(destination, IndexSectionKind.Parse, parse.Write);
			}

			if (table != null)
			{
				IndexSection.Write(destination, IndexSectionKind.Chunks, table.WriteChunks);
				IndexSection.Write(destination, IndexSectionKind.Strings, table.WriteStrings);
				IndexSection.Write(destination, IndexSectionKind.Rows, table.WriteRows);
			}

			IndexSection.Write(destination, IndexSectionKind.End, writeBytes: null);
		}

		/// <summary>
		/// Reads a saved index, provided it is intact and could belong to a log of <paramref name="logLength"/> bytes, without reading the log.
		/// </summary>
		/// <remarks>
		/// Call <see cref="LineIndex.Matches"/> to check that the content the line index scanned is unchanged.
		/// The record table's sections are skipped: see <see cref="TableOffset"/>.
		/// </remarks>
		/// <param name="lines">The line index, as saved.</param>
		/// <param name="file">What else the file holds.</param>
		public static bool TryRead(Stream indexFile, long logLength, out LineIndex lines, out IndexFile file)
		{
			lines = null;
			file = null;

			try
			{
				if (!TryReadSignature(indexFile) ||
				    !TryReadHeader(indexFile, out IndexHeader header) ||
				    header.IndexedLength > logLength ||
				    !TryReadCheckpoints(indexFile, header.CheckpointCount, out List<long> checkpoints))
				{
					return false;
				}

				SavedParse parse = null;

				if (header.HasParse && !TryReadParse(indexFile, logLength, out parse))
				{
					return false;
				}

				long tableOffset = 0;

				if (header.HasTable)
				{
					tableOffset = indexFile.Position;

					if (!IndexSection.TrySkip(indexFile, IndexSectionKind.Chunks) ||
					    !IndexSection.TrySkip(indexFile, IndexSectionKind.Strings) ||
					    !IndexSection.TrySkip(indexFile, IndexSectionKind.Rows))
					{
						return false;
					}
				}

				if (!TryReadEnd(indexFile))
				{
					return false;
				}

				lines = LineIndex.FromSaved(header, checkpoints);
				file = new IndexFile(parse, tableOffset);

				return true;
			}
			catch (Exception exception) when (exception is EndOfStreamException || exception is FormatException || exception is ArgumentException)
			{
				// A damaged file: a string's length, or a build ID, that is not what was written.
				return false;
			}
		}

		/// <summary>
		/// Reads the line count and the length of the log it covers from the header of a saved index, without checking the rest of the index or the log.
		/// </summary>
		/// <remarks>
		/// For an estimate only, such as the memory a log needs (#1028): the index may be damaged, or belong to a log that has changed since.
		/// </remarks>
		public static bool TryReadLineCount(Stream indexFile, out int lineCount, out long indexedLength)
		{
			lineCount = 0;
			indexedLength = 0;

			try
			{
				if (!TryReadSignature(indexFile) || !IndexSection.TryRead(indexFile, IndexSectionKind.Header, out IndexSection.Reader section))
				{
					return false;
				}

				using (section)
				{
					var header = IndexHeader.Read(section.Binary);
					lineCount = header.LineCount;
					indexedLength = header.IndexedLength;
				}
			}
			catch (EndOfStreamException)
			{
				return false;
			}

			// Each line but the last holds at least its line break, so a damaged header cannot claim more lines than bytes.
			return lineCount >= 0 && indexedLength >= 0 && lineCount <= indexedLength + 1;
		}

		private static bool TryReadSignature(Stream indexFile)
		{
			using var reader = new BinaryReader(indexFile, Encoding.UTF8, leaveOpen: true);

			return reader.ReadBytes(Signature.Length).AsSpan().SequenceEqual(Signature) &&
			       reader.ReadInt32() == FormatVersion;
		}

		private static bool TryReadHeader(Stream indexFile, out IndexHeader header)
		{
			header = default;

			if (!IndexSection.TryRead(indexFile, IndexSectionKind.Header, out IndexSection.Reader section))
			{
				return false;
			}

			using (section)
			{
				header = IndexHeader.Read(section.Binary);

				return section.IsIntact && header.IsConsistent;
			}
		}

		private static bool TryReadCheckpoints(Stream indexFile, int count, out List<long> checkpoints)
		{
			checkpoints = null;

			if (!IndexSection.TryRead(indexFile, IndexSectionKind.Checkpoints, out IndexSection.Reader section))
			{
				return false;
			}

			using (section)
			{
				// Checked before the list is made: the checksum detects damage, not a file made to claim billions of lines.
				if (section.Remaining != count * (long)sizeof(long))
				{
					return false;
				}

				checkpoints = new List<long>(count);

				for (var i = 0; i < count; i++)
				{
					checkpoints.Add(section.Binary.ReadInt64());
				}

				return section.IsIntact;
			}
		}

		private static bool TryReadParse(Stream indexFile, long logLength, out SavedParse parse)
		{
			parse = null;

			if (!IndexSection.TryRead(indexFile, IndexSectionKind.Parse, out IndexSection.Reader section))
			{
				return false;
			}

			using (section)
			{
				parse = SavedParse.Read(section, logLength);

				return parse is not null && section.IsIntact;
			}
		}

		private static bool TryReadEnd(Stream indexFile)
		{
			if (!IndexSection.TryRead(indexFile, IndexSectionKind.End, out IndexSection.Reader section))
			{
				return false;
			}

			using (section)
			{
				return section.IsIntact && indexFile.Position == indexFile.Length;
			}
		}
	}
}
