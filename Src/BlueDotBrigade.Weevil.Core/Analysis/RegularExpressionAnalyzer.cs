namespace BlueDotBrigade.Weevil.Analysis
{
	using System.Collections.Immutable;
	using System.Linq;
	using System.Threading;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Filter;
	using BlueDotBrigade.Weevil.Filter.Expressions;
	using BlueDotBrigade.Weevil.IO;

	/// <summary>
	/// An analyzer of the expressions the user enters, with the include filter's expression as the default: regular expressions,
	/// or any expression for Matching Record Runs.
	/// </summary>
	/// <remarks>
	/// An analyzer reads a row's text with <see cref="RecordList.ReadContent(int, ContentBuffer)"/>, and creates a row's record
	/// only to flag it or comment on it (#1150, #1258). Matching Record Runs is the exception: its expressions, such as <c>@Pinned</c>, may read the record.
	/// </remarks>
	internal abstract class RegularExpressionAnalyzer : IRecordAnalyzer
	{
		private readonly FilterStrategy _filterStrategy;
		private readonly IFilterAliasExpander _aliasExpander;

		protected RegularExpressionAnalyzer(FilterStrategy filterStrategy, IFilterAliasExpander aliasExpander)
		{
			_filterStrategy = filterStrategy;
			_aliasExpander = aliasExpander;
		}

		public abstract string Key { get; }

		public abstract string DisplayName { get; }

		/// <summary>
		/// The include filter's expression, which the dialog offers, or an empty string when there is none.
		/// </summary>
		protected string DefaultExpression =>
			_filterStrategy != FilterStrategy.KeepAllRecords && _filterStrategy.InclusiveFilter.Count > 0
				? _filterStrategy.FilterCriteria.Include
				: string.Empty;

		public Results Analyze(RecordList records, IRecordState state, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata)
		{
			return Analyze(records, state, outputDirectory, userDialog, canUpdateMetadata, CancellationToken.None);
		}

		/// <remarks>
		/// Stops within one record when <paramref name="cancellationToken"/> is cancelled, and keeps the flags and comments already changed (#1027).
		/// Flags are cleared for every row before the first is read, so a stopped analysis leaves the rows it did not reach unflagged (#1258).
		/// </remarks>
		public abstract Results Analyze(RecordList records, IRecordState state, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata, CancellationToken cancellationToken);

		/// <summary>
		/// Asks the user for the expressions, and parses them.
		/// </summary>
		/// <returns><see langword="false"/> when the user cancels, or enters no expression of type <typeparamref name="TExpression"/>.</returns>
		protected bool TryGetExpressions<TExpression>(RecordList records, IUserDialog userDialog, out ImmutableArray<TExpression> expressions)
			where TExpression : IExpression
		{
			expressions = userDialog.TryGetExpressions(this.DefaultExpression, records.Count.ToString("N0"), out var text)
				? ParseExpressions<TExpression>(text)
				: ImmutableArray<TExpression>.Empty;

			return !expressions.IsEmpty;
		}

		/// <summary>
		/// Expands the aliases in <paramref name="text"/>, and returns its expressions of type <typeparamref name="TExpression"/>, separated by <c>||</c>.
		/// </summary>
		protected ImmutableArray<TExpression> ParseExpressions<TExpression>(string text)
			where TExpression : IExpression
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return ImmutableArray<TExpression>.Empty;
			}

			return AnalyzerExpressionHelper
				.ParseAllExpressions(text, _aliasExpander, _filterStrategy.GetAnalyzerExpressionBuilder())
				.OfType<TExpression>()
				.ToImmutableArray();
		}

		/// <summary>
		/// Unflags every row in one step, by line number, so no record is created (#1150).
		/// </summary>
		protected static void ClearFlags(RecordList records, IRecordState state, bool canUpdateMetadata)
		{
			if (canUpdateMetadata)
			{
				state.SetFlagged(records, false);
			}
		}

		/// <summary>
		/// Flags the row at <paramref name="position"/>, and adds <paramref name="comment"/> to its comment. Only then is its record created.
		/// </summary>
		protected static void Flag(RecordList records, int position, IRecordState state, string comment, bool canUpdateMetadata)
		{
			if (canUpdateMetadata)
			{
				IRecord record = records[position];

				state.SetFlagged(record, true);
				state.AppendComment(record, comment);
			}
		}
	}
}
