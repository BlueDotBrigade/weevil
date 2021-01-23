namespace BlueDotBrigade.Weevil.Data
{
	using System;

	public interface IRecord
	{
		string Content { get; }

		bool HasCreationTime { get; }

		/// <summary>
		/// Indicates when the record was persisted to disk.
		/// </summary>
		/// <remarks>
		/// When the creation time cannot be determined, the property is assigned a value of <see cref="DateTime.MaxValue"/>.
		/// </remarks>
		DateTime CreatedAt { get; }

		bool HasContent { get; }

		int LineNumber { get; }

		Metadata Metadata { get; }

		SeverityType Severity { get; }
	}
}