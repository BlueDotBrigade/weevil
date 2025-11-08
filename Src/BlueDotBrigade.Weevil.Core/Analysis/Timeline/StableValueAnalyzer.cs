namespace BlueDotBrigade.Weevil.Analysis.Timeline
{
        using System.Collections.Generic;
        using System.Collections.Immutable;
        using System.Linq;
        using BlueDotBrigade.Weevil.IO;
        using Data;
        using Filter;
        using Filter.Expressions.Regular;

        internal class StableValueAnalyzer : IRecordAnalyzer
        {
                private readonly FilterStrategy _filterStrategy;

                public StableValueAnalyzer(FilterStrategy filterStrategy)
                {
                        _filterStrategy = filterStrategy;
                }

                public string Key => AnalysisType.DetectStableValues.ToString();

                public string DisplayName => "Detect Stable Values";

                public Results Analyze(ImmutableArray<IRecord> records, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata)
                {
                        var count = 0;

                        if (_filterStrategy != FilterStrategy.KeepAllRecords)
                        {
                                if (_filterStrategy.InclusiveFilter.Count > 0)
                                {
                                        ImmutableArray<RegularExpression> expressions = _filterStrategy.InclusiveFilter.GetRegularExpressions();

                                        if (!expressions.IsDefaultOrEmpty)
                                        {
                                                var activeRuns = new Dictionary<string, ValueRun>();

                                                foreach (IRecord record in records)
                                                {
                                                        if (canUpdateMetadata)
                                                        {
                                                                record.Metadata.IsFlagged = false;
                                                        }

                                                        var matchedKeys = new HashSet<string>();

                                                        foreach (RegularExpression expression in expressions)
                                                        {
                                                                IDictionary<string, string> keyValuePairs = expression.GetKeyValuePairs(record);

                                                                if (keyValuePairs.Count == 0)
                                                                {
                                                                        continue;
                                                                }

                                                                foreach (KeyValuePair<string, string> currentState in keyValuePairs)
                                                                {
                                                                        if (string.IsNullOrWhiteSpace(currentState.Value))
                                                                        {
                                                                                if (activeRuns.TryGetValue(currentState.Key, out var run))
                                                                                {
                                                                                        FinalizeRun(currentState.Key, run);
                                                                                }

                                                                                continue;
                                                                        }

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

                                                        count++;
                                                        if (canUpdateMetadata)
                                                        {
                                                                record.Metadata.IsFlagged = true;
                                                                record.Metadata.UpdateUserComment($"Start {friendlyName}: {value}");
                                                        }
                                                }

                                                void FinalizeRun(string key, ValueRun run)
                                                {
                                                        count++;
                                                        if (canUpdateMetadata)
                                                        {
                                                                run.LastRecord.Metadata.IsFlagged = true;
                                                                run.LastRecord.Metadata.UpdateUserComment($"Stop {run.FriendlyName}: {run.Value}");
                                                        }

                                                        activeRuns.Remove(key);
                                                }
                                        }
                                }
                        }

                        return new Results(count);
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
