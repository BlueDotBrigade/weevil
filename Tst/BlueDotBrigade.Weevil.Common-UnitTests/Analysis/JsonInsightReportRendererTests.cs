namespace BlueDotBrigade.Weevil.Common.Analysis
{
	using System;
	using System.Collections.Generic;
	using System.Collections.Immutable;
	using global::BlueDotBrigade.Weevil.Analysis;

	[TestClass]
	public class JsonInsightReportRendererTests
	{
		[TestMethod]
		public void GivenAReport_WhenRenderedAsJson_ThenItFollowsTheContract()
		{
			// Programs rely on this shape: Doc/Design/InsightReport.md describes it (#837).
			var report = new InsightReport
			{
				SourceFileName = "Application.log",
				Context = new Dictionary<string, string> { { "Application", "Droid" } },
				WeevilVersion = "3.0.1",
				GeneratedAt = new DateTimeOffset(2026, 11, 12, 9, 30, 0, TimeSpan.FromHours(-5)),
				From = new DateTime(2026, 5, 6, 10, 0, 0),
				To = null,
				RecordCount = 13,
				FileSize = 2048,
				Metrics = ImmutableArray.Create(
					new InsightReportMetric { Name = "Errors", Value = 1 },
					new InsightReportMetric { Name = "Threshold", Value = TimeSpan.FromSeconds(5) },
					new InsightReportMetric { Name = "FirstOccurredAt", Value = null }),
				Insights = ImmutableArray.Create(
					new InsightReportItem
					{
						Title = "Critical Errors",
						MetricValue = "3",
						MetricUnit = "Σ",
						Details = "Critical errors found, see 'Help'",
						IsAttentionRequired = true,
						RelatedLineNumbers = ImmutableArray.Create(42, 7),
					}),
			};

			var json = new JsonInsightReportRenderer().Render(report);

			json.ReplaceLineEndings().Should().Be(string.Join(Environment.NewLine, new[]
			{
				"{",
				"  \"schemaVersion\": 1,",
				"  \"sourceFileName\": \"Application.log\",",
				"  \"context\": {",
				"    \"Application\": \"Droid\"",
				"  },",
				"  \"weevilVersion\": \"3.0.1\",",
				"  \"generatedAt\": \"2026-11-12T09:30:00-05:00\",",
				"  \"from\": \"2026-05-06T10:00:00\",",
				"  \"to\": null,",
				"  \"recordCount\": 13,",
				"  \"fileSize\": 2048,",
				"  \"metrics\": [",
				"    {",
				"      \"name\": \"Errors\",",
				"      \"value\": 1",
				"    },",
				"    {",
				"      \"name\": \"Threshold\",",
				"      \"value\": \"00:00:05\"",
				"    },",
				"    {",
				"      \"name\": \"FirstOccurredAt\",",
				"      \"value\": null",
				"    }",
				"  ],",
				"  \"insights\": [",
				"    {",
				"      \"title\": \"Critical Errors\",",
				"      \"metricValue\": \"3\",",
				"      \"metricUnit\": \"Σ\",",
				"      \"details\": \"Critical errors found, see 'Help'\",",
				"      \"isAttentionRequired\": true,",
				"      \"relatedLineNumbers\": [",
				"        42,",
				"        7",
				"      ]",
				"    }",
				"  ]",
				"}",
			}));
		}
	}
}
