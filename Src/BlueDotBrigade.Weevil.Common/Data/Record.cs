namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;

	/// <threadsafety static="true" instance="true" />
	[DebuggerDisplay("LineNumber={" + nameof(LineNumber) + "}")]
	public sealed class Record : IRecord
	{
		/// <summary>
		/// Represents a surrogate that is used when a genuine record is not available.
		/// </summary>
		[SuppressMessage("Microsoft.Security", "CA2104")]
		public static readonly Record Dummy = new Record(
			-1,
			CreationTimeUnknown,
			SeverityType.Verbose,
			string.Empty,
			new Metadata());

		/// <summary>
		/// Indicates the system was unable to determine when the record was generated.
		/// </summary>
		public static readonly DateTime CreationTimeUnknown = DateTime.MaxValue;

		[Obsolete("To minimize bugs in production, this constructor will be removed in a future release.")]
		public Record(int lineNumber)
		: this(lineNumber, DateTime.Now, Dummy.Severity, Dummy.Content, Dummy.Metadata)
		{
			// nothing to do
		}

		public Record(int lineNumber, DateTime createdAt, SeverityType severity, string content)
		: this(lineNumber, createdAt, severity, content, new Metadata())
		{
			// nothing to do
		}

		public Record(int lineNumber, DateTime createdAt, SeverityType severity, string content,
			 Metadata metadata)
		{
			this.LineNumber = lineNumber;
			this.CreatedAt = createdAt;
			this.Severity = severity;
			this.Content = content ?? string.Empty;
			this.Metadata = metadata;
		}

		public int LineNumber { get; }

		public DateTime CreatedAt { get; }

		public SeverityType Severity { get; }

		public string Content { get; }

		public bool HasContent => !string.IsNullOrWhiteSpace(this.Content);

		public Metadata Metadata { get; }

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
		///     Returns <see langword="true" /> when the parameter refers to <see langword="null" /> or <see cref="Record.Dummy" />
		///     .
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

		public override string ToString()
		{
			return $@"LineNumber={this.LineNumber}, Severity={this.Severity}, HasContent={this.HasContent}, CreatedAt={this.CreatedAt}, Metadata={{{this.Metadata.ToString()}}}";
		}
	}
}