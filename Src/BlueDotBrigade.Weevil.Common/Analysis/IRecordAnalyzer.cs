namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Threading;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.IO;

	/// <seealso cref="ICalculator"/>
	/// <seealso cref="IMetricCollector"/>
	public interface IRecordAnalyzer
	{
		/// <summary>
		/// Represents a unique value that can be used to reference a <see cref="IRecordAnalyzer"/>.
		/// </summary>
		string Key { get; }

		/// <summary>
		/// Represents the analyzer's name that will be displayed on the user interface.
		/// </summary>
		string DisplayName { get; }

		/// <summary>
		/// Looks for trends within the provided <see cref="IRecord"/> collection.
		/// </summary>
		/// <param name="records">List of records to be analyzed.</param>
		/// <param name="state">The records' comments, pins and flags, which the analyzer may change.</param>
		/// <param name="outputDirectory">Path to where analysis results can be saved.</param>
		/// <param name="userDialog">Enables the analyzer to prompt the user for additional context.</param>
		/// <param name="canUpdateMetadata">When <see langword="false"/>, the analyzer does not change <paramref name="state"/>.</param>
		/// <remarks>
		/// Read each record's content through one <see cref="ContentBuffer"/> for the whole pass, with <see cref="ContentBuffer.Read(IRecord)"/>
		/// or <see cref="RecordList.ReadContent(int, ContentBuffer)"/>. For a log opened on disk, <see cref="IRecord.Content"/> reads the file for every record,
		/// while a buffer reads each part of the log once (#1027).
		/// </remarks>
		Results Analyze(RecordList records, IRecordState state, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata);

		/// <summary>
		/// Looks for trends within the provided <see cref="IRecord"/> collection, and stops when <paramref name="cancellationToken"/> is cancelled.
		/// </summary>
		/// <remarks>
		/// Abort cancels the token (#1027). An analyzer that stops throws <see cref="OperationCanceledException"/>, and keeps the comments, pins and flags it changed so far.
		/// This default checks the token only before it calls the overload without one, so an analyzer that reads many records, such as one of a log on disk,
		/// implements it and checks the token at each record.
		/// </remarks>
		/// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
		Results Analyze(RecordList records, IRecordState state, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();

			return Analyze(records, state, outputDirectory, userDialog, canUpdateMetadata);
		}
	}
}
