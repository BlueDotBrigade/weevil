namespace BlueDotBrigade.Weevil.Common.Analysis
{
	using System;
	using System.Collections.Immutable;
	using System.Text.Json;
	using System.Xml.Linq;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.IO;
	using FluentAssertions;
	using NSubstitute;

	[TestClass]
	public class InsightReportGeneratorTests
	{
		[TestMethod]
		public void GivenInsights_WhenGenerateCalled_ThenBuildsStructuredReportWithProblemAreasAndMoreInformation()
		{
			// Regression: Issue #837
			var relatedRecord = R.Create()
				.WithCreatedAt(0, "10:00:00")
				.GetRecords()[0];
			var insightWithAttention = new FakeInsight(
				title: "Critical Errors",
				metricValue: "3",
				metricUnit: "Σ",
				details: "Critical errors found",
				isAttentionRequired: true,
				relatedRecords: [Record.Dummy, relatedRecord]);
			var informationalInsight = new FakeInsight(
				title: "Warnings",
				metricValue: "10",
				metricUnit: "Σ",
				details: "Warning records detected",
				isAttentionRequired: false,
				relatedRecords: []);
			var engine = Substitute.For<IEngine>();
			engine.SourceFilePath.Returns("/var/log/Application.log");
			engine.Context.Returns(ContextDictionary.Empty);

			var report = new InsightReportGenerator().Generate(
				new Version(1, 2, 3),
				engine,
				[insightWithAttention, informationalInsight],
				DateTime.Parse("2026-05-06T10:00:00Z"),
				DateTime.Parse("2026-05-06T11:00:00Z"));

			report.Title.Should().Be("Weevil Insight: Application.log");
			report.Context.Should().Be("Not specified");
			report.ProblemAreas.Should().HaveCount(1);
			report.MoreInformation.Should().HaveCount(1);
			report.ProblemAreas[0].RelatedRecords.Should().HaveCount(1);
			report.ProblemAreas[0].RelatedRecords[0].LineNumber.Should().Be(0);
		}

		[TestMethod]
		public void GivenJsonFormatter_WhenFactoryCreatesRenderer_ThenRendererOutputsStructuredReportJson()
		{
			// Regression: Issue #837
			var report = CreateReport();
			var renderer = InsightReportRendererFactory.Create(new JsonFormatter());

			var output = renderer.Render(report);

			using var json = JsonDocument.Parse(output);
			json.RootElement.GetProperty("title").GetString().Should().Be(report.Title);
			json.RootElement.GetProperty("problemAreas").GetArrayLength().Should().Be(1);
			json.RootElement.TryGetProperty("heading", out _).Should().BeFalse();
		}

		[TestMethod]
		public void GivenXmlFormatter_WhenFactoryCreatesRenderer_ThenRendererOutputsStructuredReportXml()
		{
			// Regression: Issue #837
			var report = CreateReport();
			var renderer = InsightReportRendererFactory.Create(new XmlFormatter());

			var output = renderer.Render(report);

			var xml = XDocument.Parse(output);
			xml.Root.Should().NotBeNull();
			xml.Root!.Name.LocalName.Should().Be("InsightReport");
			xml.Root.Element("ProblemAreas")!.Elements("Insight").Should().HaveCount(1);
		}

		private static InsightReport CreateReport()
		{
			return new InsightReport
			{
				Title = "Weevil Insight: Application.log",
				SourceFileName = "Application.log",
				Context = "Not specified",
				WeevilVersion = new Version(1, 2, 3),
				From = DateTime.Parse("2026-05-06T10:00:00Z"),
				To = DateTime.Parse("2026-05-06T11:00:00Z"),
				ProblemAreas =
				[
					new InsightReportItem
					{
						Title = "Critical Errors",
						MetricValue = "3",
						MetricUnit = "Σ",
						Details = "Critical errors found",
						IsAttentionRequired = true
					}
				],
				MoreInformation = ImmutableArray<InsightReportItem>.Empty
			};
		}

		private sealed class FakeInsight : IInsight
		{
			public FakeInsight(
				string title,
				string metricValue,
				string metricUnit,
				string details,
				bool isAttentionRequired,
				ImmutableArray<IRecord> relatedRecords)
			{
				Title = title;
				MetricValue = metricValue;
				MetricUnit = metricUnit;
				Details = details;
				IsAttentionRequired = isAttentionRequired;
				RelatedRecords = relatedRecords;
			}

			public string Title { get; }
			public string MetricValue { get; }
			public string MetricUnit { get; }
			public string Details { get; }
			public bool IsAttentionRequired { get; }
			public ImmutableArray<IRecord> RelatedRecords { get; }

			public void Refresh(ImmutableArray<IRecord> records)
			{
				// no-op for tests
			}
		}
	}
}
