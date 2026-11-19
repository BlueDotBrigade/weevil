namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.IO;

	/// <summary>
	/// Provides common functionality for timeline analyzers.
	/// </summary>
	internal static class AnalysisHelper
	{
		/// <summary>
		/// Prompts the user for the analysis order (Ascending or Descending).
		/// </summary>
		/// <param name="userDialog">The user dialog interface for prompting.</param>
		/// <returns>The selected analysis order.</returns>
		/// <exception cref="ArgumentOutOfRangeException">Thrown when the user input is invalid.</exception>
		public static AnalysisOrder GetAnalysisOrder(IUserDialog userDialog)
		{
			var userInput = userDialog.ShowUserPrompt(
				"Analysis Details",
				"Analysis order (Ascending/Descending):",
				"Ascending");

			if (Enum.TryParse(userInput, true, out AnalysisOrder direction))
			{
				return direction;
			}

			throw new ArgumentOutOfRangeException(
				nameof(userInput),
				userInput,
				"Unable to perform operation. The analysis order was expected to be either: Ascending or Descending");
		}

		/// <summary>
		/// Orders records chronologically for timeline analysis.
		/// </summary>
		/// <remarks>
		/// Records without timestamps are ignored because they cannot be placed in a deterministic chronological order.
		/// Records sharing the same timestamp are ordered by <see cref="IRecord.LineNumber"/> to preserve file order.
		/// The positions are sorted, not the records, so no record is created or copied (#1137).
		/// </remarks>
		public static RecordList OrderRecordsForAnalysis(RecordList records, AnalysisOrder analysisOrder)
		{
			IEnumerable<int> timestamped = Enumerable
				.Range(0, records.Count)
				.Where(position => records.GetCreatedAt(position) != Record.CreationTimeUnknown);

			IOrderedEnumerable<int> orderedPositions = analysisOrder == AnalysisOrder.Ascending
				? timestamped.OrderBy(records.GetCreatedAt)
				: timestamped.OrderByDescending(records.GetCreatedAt);

			return new PositionRecordList(records, orderedPositions.ThenBy(records.GetLineNumber).ToArray());
		}
	}
}
