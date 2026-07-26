namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
        using System.Collections.Generic;
        using System.Collections.Immutable;
        using System.Linq;
        using BlueDotBrigade.Weevil.IO;
        using Data;
        using Filter;
        using Filter.Expressions.Regular;

        /// <summary>
        /// Parses regex named capture groups and tracks each captured key independently across records.
        /// Flags the first and last records in every maximal contiguous run with the same captured value.
        /// A changed value finalizes the current run and starts a new one; a non-matching record finalizes
        /// the current run without starting another.
        /// </summary>
        internal class StableValueRunsAnalyzer : IRecordAnalyzer
        {
                private readonly FilterStrategy _filterStrategy;
                private readonly IFilterAliasExpander _aliasExpander;

                public StableValueRunsAnalyzer(FilterStrategy filterStrategy)
                        : this(filterStrategy, null)
                {
                }

                public StableValueRunsAnalyzer(FilterStrategy filterStrategy, IFilterAliasExpander aliasExpander)
                {
                        _filterStrategy = filterStrategy;
                        _aliasExpander = aliasExpander;
                }

                public string Key => AnalysisType.StableValueRuns.ToString();

                public string DisplayName => "Stable Value Runs";

                public Results Analyze(ImmutableArray<IRecord> records, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata)
                {
                        var flaggedLineNumbers = new HashSet<int>();

                        // Get default regex from current inclusive filter
                        var defaultRegex = AnalysisHelper.GetDefaultRegex(_filterStrategy);

                        // Show analysis dialog to get custom regex
                        var recordsDescription = records.Length.ToString("N0");

                        if (!userDialog.TryGetExpressions(defaultRegex, recordsDescription, out var customRegex))
                        {
                                // User cancelled
                                return new Results(0);
                        }

                        if (string.IsNullOrWhiteSpace(customRegex))
                        {
                                // No regex provided
                                return new Results(0);
                        }

                        // Parse expressions with alias expansion and || support
                        var expressionBuilder = _filterStrategy.GetAnalyzerExpressionBuilder();
                        ImmutableArray<RegularExpression> expressions = AnalyzerExpressionHelper.ParseExpressions(
                                customRegex,
                                _aliasExpander,
                                expressionBuilder);

                        if (!expressions.IsDefaultOrEmpty)
                        {
                                var activeRuns = new Dictionary<string, ValueRun>();

                                        foreach (IRecord record in records)
                                        {
                                                AnalysisHelper.ClearRecordFlag(record, canUpdateMetadata);

                                                var matchedKeys = new HashSet<string>();
                                                IDictionary<string, string> keyValuePairs = AnalyzerExpressionHelper.GetResolvedKeyValuePairs(expressions, record);

                                                foreach (KeyValuePair<string, string> currentState in keyValuePairs)
                                                {
                                                        matchedKeys.Add(currentState.Key);

                                                        if (activeRuns.TryGetValue(currentState.Key, out var existingRun))
                                                        {
                                                                if (existingRun.ValueEquals(currentState.Value))
                                                                {
                                                                        existingRun.UpdateLastRecord(record);
                                                                }
                                                                else
                                                                {
                                                                        FinalizeRun(currentState.Key, existingRun);
                                                                        StartNewRun(currentState.Key, currentState.Value, record);
                                                                }
                                                        }
                                                        else
                                                        {
                                                                StartNewRun(currentState.Key, currentState.Value, record);
                                                        }
                                                }

                                                        if (activeRuns.Count > 0)
                                                        {
                                                                List<string> keysToFinalize = activeRuns.Keys
                                                                        .Where(key => !matchedKeys.Contains(key))
                                                                        .ToList();

                                                                foreach (string key in keysToFinalize)
                                                                {
                                                                        if (activeRuns.TryGetValue(key, out var run))
                                                                        {
                                                                                FinalizeRun(key, run);
                                                                        }
                                                                }
                                                        }
                                                }

                                                foreach (KeyValuePair<string, ValueRun> activeRun in activeRuns.ToArray())
                                                {
                                                        FinalizeRun(activeRun.Key, activeRun.Value);
                                                }

                                                void StartNewRun(string key, string value, IRecord record)
                                                {
                                                        var friendlyName = RegularExpression.GetFriendlyParameterName(key);
                                                        var run = ValueRun.Start(friendlyName, value, record);
                                                        activeRuns[key] = run;

                                                        flaggedLineNumbers.Add(record.LineNumber);
                                                        AnalysisHelper.UpdateRecordMetadata(
                                                                record,
                                                                true,
                                                                $"Start {friendlyName}: {value}",
                                                                canUpdateMetadata);
                                                }

                                                void FinalizeRun(string key, ValueRun run)
                                                {
                                                        flaggedLineNumbers.Add(run.LastRecord.LineNumber);
                                                        AnalysisHelper.UpdateRecordMetadata(
                                                                run.LastRecord,
                                                                true,
                                                                $"Stop {run.FriendlyName}: {run.Value}",
                                                                canUpdateMetadata);

                                                        activeRuns.Remove(key);
                                                }
                        }

                        return new Results(flaggedLineNumbers.Count);
                }

                private sealed class ValueRun
                {
                        private ValueRun(string friendlyName, string value, IRecord record)
                        {
                                FriendlyName = friendlyName;
                                Value = value;
                                LastRecord = record;
                        }

                        public string FriendlyName { get; }

                        public string Value { get; }

                        public IRecord LastRecord { get; private set; }

                        public static ValueRun Start(string friendlyName, string value, IRecord record)
                        {
                                return new ValueRun(friendlyName, value, record);
                        }

                        public bool ValueEquals(string value)
                        {
                                return Value == value;
                        }

                        public void UpdateLastRecord(IRecord record)
                        {
                                LastRecord = record;
                        }
                }
        }
}
