namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;

	//internal class Insight : IInsight
	//{
		
	//	public static readonly IInsight None = new Insight();

	//	private Insight()
	//	{
	//		this.Title = "IMPORTANT METRIC";
	//		this.MetricValue = "999";
	//		this.MetricUnit = "max";
	//		this.Details =
	//			"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Primum cur ista res digna odio est, nisi quod est turpis?";
	//		this.IsAttentionRequired = true;
	//	}

	//	public Insight(string title, string metricValue, string metricUnit, string details, bool isAttentionRequired)
	//	{
	//		this.Title = title.ToUpper() ?? throw new ArgumentNullException(nameof(title));
	//		this.MetricValue = metricValue ?? throw new ArgumentNullException(nameof(metricValue));
	//		this.MetricUnit = metricUnit ?? throw new ArgumentNullException(nameof(metricUnit));
	//		this.Details = details ?? throw new ArgumentNullException(nameof(details));
	//		this.IsAttentionRequired = isAttentionRequired;
	//	}

	//	public string Title { get; private set; }
	//	public string MetricValue { get; private set; }
	//	public string MetricUnit { get; private set; }
	//	public string Details { get; private set; }
	//	public bool IsAttentionRequired { get; private set; }
	//	public void Refresh(ImmutableArray<IRecord> records)
	//	{
	//		throw new NotImplementedException();
	//	}
	//}
}
