namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System;
	using System.Collections.Immutable;
	using System.Linq;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.IO;
	using Filter;
	using Filter.Expressions.Regular;

	/// <summary>
	/// Provides common functionality for timeline analyzers.
	/// </summary>
	internal static class AnalysisHelper
	{
		/// <summary>
		/// Validates that the filter strategy has the necessary conditions for analysis.
		/// </summary>
		/// <param name="filterStrategy">The filter strategy to validate.</param>
		/// <returns>True if the filter strategy is valid for analysis; otherwise, false.</returns>
		public static bool CanPerformAnalysis(FilterStrategy filterStrategy)
		{
			return filterStrategy != FilterStrategy.KeepAllRecords 
				&& filterStrategy.InclusiveFilter.Count > 0;
		}

		/// <summary>
		/// Gets the regular expressions from the filter strategy for analysis.
		/// </summary>
		/// <param name="filterStrategy">The filter strategy containing the expressions.</param>
		/// <returns>An immutable array of regular expressions.</returns>
		public static ImmutableArray<RegularExpression> GetRegularExpressions(FilterStrategy filterStrategy)
		{
			return filterStrategy.InclusiveFilter.GetRegularExpressions();
		}

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
		/// </remarks>
		public static RecordList OrderRecordsForAnalysis(RecordList records, AnalysisOrder analysisOrder)
		{
			IOrderedEnumerable<IRecord> orderedRecords = analysisOrder == AnalysisOrder.Ascending
				? records
					.Where(record => record.HasCreationTime)
					.OrderBy(record => record.CreatedAt)
				: records
					.Where(record => record.HasCreationTime)
					.OrderByDescending(record => record.CreatedAt);

			return orderedRecords
				.ThenBy(record => record.LineNumber)
				.ToImmutableArray();
		}

		/// <summary>
		/// Flags a record and adds to its comment, if updates are allowed.
		/// </summary>
		/// <param name="record">The record to update.</param>
		/// <param name="state">The records' comments and flags.</param>
		/// <param name="isFlagged">Whether the record should be flagged.</param>
		/// <param name="comment">The comment to add to the record.</param>
		/// <param name="canUpdateMetadata">Whether updates are allowed.</param>
		public static void UpdateRecordMetadata(IRecord record, IRecordState state, bool isFlagged, string comment, bool canUpdateMetadata)
		{
			if (canUpdateMetadata)
			{
				state.SetFlagged(record, isFlagged);
				if (!string.IsNullOrEmpty(comment))
				{
					state.AppendComment(record, comment);
				}
			}
		}

		/// <summary>
		/// Clears the flagged status of a record if updates are allowed.
		/// </summary>
		/// <param name="record">The record to update.</param>
		/// <param name="state">The records' flags.</param>
		/// <param name="canUpdateMetadata">Whether updates are allowed.</param>
		public static void ClearRecordFlag(IRecord record, IRecordState state, bool canUpdateMetadata)
		{
			if (canUpdateMetadata)
			{
				state.SetFlagged(record, false);
			}
		}

		/// <summary>
		/// Gets the default regex from the inclusive filter of the filter strategy.
		/// </summary>
		/// <param name="filterStrategy">The filter strategy containing the filter criteria.</param>
		/// <returns>The include filter regex if available; otherwise, an empty string.</returns>
		public static string GetDefaultRegex(FilterStrategy filterStrategy)
		{
			if (filterStrategy != FilterStrategy.KeepAllRecords && filterStrategy.InclusiveFilter.Count > 0)
			{
				return filterStrategy.FilterCriteria.Include;
			}
			return string.Empty;
		}
	}
}
