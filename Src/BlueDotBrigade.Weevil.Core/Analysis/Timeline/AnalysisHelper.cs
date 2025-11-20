namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
	using System;
	using System.Collections.Immutable;
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
				$"{nameof(direction)}",
				direction,
				"Unable to perform operation. The analysis order was expected to be either: Ascending or Descending");
		}

		/// <summary>
		/// Updates the metadata for a record if metadata updates are allowed.
		/// </summary>
		/// <param name="record">The record to update.</param>
		/// <param name="isFlagged">Whether the record should be flagged.</param>
		/// <param name="comment">The comment to add to the record.</param>
		/// <param name="canUpdateMetadata">Whether metadata updates are allowed.</param>
		public static void UpdateRecordMetadata(Data.IRecord record, bool isFlagged, string comment, bool canUpdateMetadata)
		{
			if (canUpdateMetadata)
			{
				record.Metadata.IsFlagged = isFlagged;
				if (!string.IsNullOrEmpty(comment))
				{
					record.Metadata.UpdateUserComment(comment);
				}
			}
		}

		/// <summary>
		/// Clears the flagged status of a record if metadata updates are allowed.
		/// </summary>
		/// <param name="record">The record to update.</param>
		/// <param name="canUpdateMetadata">Whether metadata updates are allowed.</param>
		public static void ClearRecordFlag(Data.IRecord record, bool canUpdateMetadata)
		{
			if (canUpdateMetadata)
			{
				record.Metadata.IsFlagged = false;
			}
		}
	}
}
