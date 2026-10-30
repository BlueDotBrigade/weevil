namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;

	/// <threadsafety static="true" instance="true" />
	[DebuggerDisplay("Line {" + nameof(LineNumber) + "}: {this.DebuggerString()}")]
	public sealed class Record : IRecord, IEquatable<IRecord>
	{
		/// <summary>
		/// Represents a surrogate that is used when a genuine record is not available.
		/// </summary>
		[SuppressMessage("Microsoft.Security", "CA2104")]
		public static readonly IRecord Dummy = new Record(
			-1,
			CreationTimeUnknown,
			SeverityType.Verbose,
			string.Empty);

		/// <summary>
		/// Indicates the system was unable to determine when the record was generated.
		/// </summary>
		public static readonly DateTime CreationTimeUnknown = DateTime.MaxValue;

		// The facts the parser found, in one byte: the severity in the low bits, then one bit per flag.
		private const byte SeverityMask = 0b0000_0111;
		private const byte MultiLineFlag = 0b0000_1000;
		private const byte UserInterfaceThreadFlag = 0b0001_0000;

		private readonly byte _facts;

		// The content: a string, or the chunk of the log's bytes that holds it (#1133).
		private readonly object _content;
		private readonly int _contentOffset;
		private readonly int _contentLength;

		public Record(int lineNumber, DateTime createdAt, SeverityType severity, string content)
		: this(lineNumber, createdAt, severity, content, isMultiLine: false, wasGeneratedByUi: false)
		{
			// nothing to do
		}

		/// <param name="isMultiLine">The record spans several lines of the log.</param>
		/// <param name="wasGeneratedByUi">The record was written by the application's user interface thread.</param>
		public Record(int lineNumber, DateTime createdAt, SeverityType severity, string content,
			 bool isMultiLine, bool wasGeneratedByUi)
		{
			this.LineNumber = lineNumber;
			this.CreatedAt = createdAt;
			_content = content ?? string.Empty;
			_facts = GetFacts(severity, isMultiLine, wasGeneratedByUi);
		}

		/// <summary>
		/// Creates a record whose content is the bytes at <paramref name="contentOffset"/> in <paramref name="chunk"/>, decoded when it is read (#1133).
		/// </summary>
		/// <param name="isMultiLine">The record spans several lines of the log, which <see cref="Content"/> joins with <c>\r\n</c>.</param>
		/// <param name="wasGeneratedByUi">The record was written by the application's user interface thread.</param>
		public Record(int lineNumber, DateTime createdAt, SeverityType severity, ContentChunk chunk, int contentOffset, int contentLength,
			 bool isMultiLine, bool wasGeneratedByUi)
		{
			ArgumentNullException.ThrowIfNull(chunk);

			if (contentOffset < 0 || contentLength < 0 || contentOffset > chunk.Length - contentLength)
			{
				throw new ArgumentOutOfRangeException(nameof(contentOffset), contentOffset, "The content is expected to be within the chunk.");
			}

			this.LineNumber = lineNumber;
			this.CreatedAt = createdAt;
			_content = chunk;
			_contentOffset = contentOffset;
			_contentLength = contentLength;
			_facts = GetFacts(severity, isMultiLine, wasGeneratedByUi);
		}

		private static byte GetFacts(SeverityType severity, bool isMultiLine, bool wasGeneratedByUi)
		{
			if ((uint)severity > SeverityMask)
			{
				throw new ArgumentOutOfRangeException(nameof(severity), severity, "The severity is not a known value.");
			}

			return (byte)((byte)severity
				| (isMultiLine ? MultiLineFlag : 0)
				| (wasGeneratedByUi ? UserInterfaceThreadFlag : 0));
		}

		public int LineNumber { get; }

		public DateTime CreatedAt { get; }

		public SeverityType Severity => (SeverityType)(_facts & SeverityMask);

		public bool IsMultiLine => (_facts & MultiLineFlag) != 0;

		public bool WasGeneratedByUi => (_facts & UserInterfaceThreadFlag) != 0;

		/// <remarks>
		/// When the record holds the log's bytes, each read decodes them into a new string. To read many records, use a <see cref="ContentBuffer"/>.
		/// </remarks>
		public string Content => _content as string ?? ((ContentChunk)_content).GetString(_contentOffset, _contentLength, this.IsMultiLine);

		public bool HasContent => !string.IsNullOrWhiteSpace(this.Content);

#if DEBUG
		/// <summary>
		/// Returns a new record with the same line, facts and content, as a record list over a record table will (#1136).
		/// </summary>
		internal Record Copy()
		{
			return _content is ContentChunk chunk
				? new Record(this.LineNumber, this.CreatedAt, this.Severity, chunk, _contentOffset, _contentLength, this.IsMultiLine, this.WasGeneratedByUi)
				: new Record(this.LineNumber, this.CreatedAt, this.Severity, (string)_content, this.IsMultiLine, this.WasGeneratedByUi);
		}
#endif

		/// <summary>
		/// Decodes the content into <paramref name="buffer"/>, which grows when needed, if the record holds the log's bytes.
		/// </summary>
		/// <returns><see langword="false"/> when the record holds a string, which <see cref="Content"/> returns as it is.</returns>
		internal bool TryDecodeContent(ref char[] buffer, out int length)
		{
			if (_content is ContentChunk chunk)
			{
				length = chunk.Decode(_contentOffset, _contentLength, this.IsMultiLine, ref buffer);
				return true;
			}

			length = 0;
			return false;
		}

		/// <summary>
		/// Returns <see langword="true"/> if it is known when the record was created.
		/// </summary>
		public static bool ValidateCreationTime(IRecord record)
		{
			if (record is null)
			{
				throw new ArgumentNullException(nameof(record));
			}

			return record.CreatedAt != CreationTimeUnknown;
		}

		public bool HasCreationTime => Record.ValidateCreationTime(this);

		/// <summary>
		/// Indicates whether the provided value refers to a dummy record, or a null value.
		/// </summary>
		/// <returns>
		///     Returns <see langword="true" /> when the parameter refers to <see langword="null" /> or <see cref="Record.Dummy" />.
		///     Records are equal by line number, so any <see cref="Record" /> on line −1, the dummy's line, is the dummy; a log's line numbers start at 1 (#1135).
		/// </returns>
		public static bool IsDummyOrNull(IRecord record)
		{
			return record?.Equals(Record.Dummy) ?? true;
		}

		/// <summary>
		/// Indicates whether the parameter refers to an authentic record. 
		/// </summary>
		/// Returns
		/// <see langword="true" />
		/// when the provided
		/// <see cref="Record" />
		/// is not
		/// <see langword="null" />
		/// or
		/// <see cref="Record.Dummy" />
		/// .
		public static bool IsGenuine(IRecord record)
		{
			return !IsDummyOrNull(record);
		}

		/// <summary>
		/// A record is equal to any record with the same line number, which identifies a record within its log (#1135).
		/// </summary>
		/// <remarks>
		/// Records will be created when they are read (#1021), so one line can give several record objects.
		/// Records of different logs with the same line number are equal too: Weevil compares records only within one log.
		/// </remarks>
		public bool Equals(IRecord other)
		{
			return other is not null && other.LineNumber == this.LineNumber;
		}

		public override bool Equals(object obj)
		{
			return obj is IRecord other && Equals(other);
		}

		public override int GetHashCode()
		{
			return this.LineNumber;
		}

		private string DebuggerString()
		{
			return this.Content.Length <= 64 ? this.Content : this.Content.Substring(0, 64);
		}

		public override string ToString()
		{
			return $@"LineNumber={this.LineNumber}, Severity={this.Severity}, HasContent={this.HasContent}, CreatedAt={this.CreatedAt}, IsMultiLine={this.IsMultiLine}, WasGeneratedByUi={this.WasGeneratedByUi}";
		}
	}
}
