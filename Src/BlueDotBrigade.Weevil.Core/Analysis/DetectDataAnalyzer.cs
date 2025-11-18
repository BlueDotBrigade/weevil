namespace BlueDotBrigade.Weevil.Analysis
{
using System.Collections.Generic;
using System.Collections.Immutable;
using BlueDotBrigade.Weevil.IO;
using Data;
using Filter;
using Filter.Expressions.Regular;

internal class DetectDataAnalyzer : IRecordAnalyzer
{
private readonly FilterStrategy _filterStrategy;

public DetectDataAnalyzer(FilterStrategy filterStrategy)
{
_filterStrategy = filterStrategy;
}

public string Key => AnalysisType.DetectData.ToString();

public string DisplayName => "Detect Data";

/// <summary>
/// Extracts key/value pairs defined by regular expression "groups", and then updates the corresponding <see cref="Metadata.Comment"/>.
/// </summary>
/// <see href="https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions">MSDN: Defining RegEx Groups</see>
public Results Analyze(ImmutableArray<IRecord> records, string outputDirectory, IUserDialog userDialog, bool canUpdateMetadata)
{
var count = 0;

// Get default regex from current inclusive filter
var defaultRegex = string.Empty;
if (_filterStrategy != FilterStrategy.KeepAllRecords && _filterStrategy.InclusiveFilter.Count > 0)
{
defaultRegex = _filterStrategy.FilterCriteria.Include;
}

// Show analysis dialog to get custom regex
var recordsDescription = records.Length.ToString("N0");

if (!userDialog.TryShowAnalysisDialog(defaultRegex, recordsDescription, out var customRegex))
{
// User cancelled
return new Results(0);
}

if (string.IsNullOrWhiteSpace(customRegex))
{
// No regex provided
return new Results(0);
}

// Create expression from custom regex
var expressionBuilder = _filterStrategy.GetExpressionBuilder();
if (expressionBuilder.TryGetExpression(customRegex, out var expression))
{
if (expression is RegularExpression regexExpression)
{
foreach (IRecord record in records)
{
if (canUpdateMetadata)
{
record.Metadata.IsFlagged = false;
}

IDictionary<string, string> keyValuePairs = regexExpression.GetKeyValuePairs(record);

if (keyValuePairs.Count > 0)
{
foreach (KeyValuePair<string, string> keyValuePair in keyValuePairs)
{
if (!string.IsNullOrWhiteSpace(keyValuePair.Value))
{
var parameterName = RegularExpression.GetFriendlyParameterName(keyValuePair.Key);

if (canUpdateMetadata)
{
record.Metadata.IsFlagged = true;
record.Metadata.UpdateUserComment($"{parameterName}: {keyValuePair.Value}");
}

count++;
}
}
}
}
}
}

return new Results(count);
}

{
var parameterName = RegularExpression.GetFriendlyParameterName(keyValuePair.Key);

if (canUpdateMetadata)
{
record.Metadata.IsFlagged = true;
record.Metadata.UpdateUserComment($"{parameterName}: {keyValuePair.Value}");
}

count++;
}
}
}
}
}
}

return new Results(count);
}
}
}
