namespace BlueDotBrigade.Weevil.Analysis
{
	using System;
	using System.Collections.Immutable;
	using BlueDotBrigade.Weevil.Data;

	public abstract class InsightBase : IInsight
	{
		private readonly string _defaultMetricValue;
		private readonly string _defaultDetails;

		protected InsightBase(string title, string metricUnit, string metricValue, string defaultDetails)
		{
			_defaultMetricValue = metricValue ?? throw new ArgumentNullException(nameof(metricValue));
			_defaultDetails = defaultDetails ?? throw new ArgumentNullException(nameof(defaultDetails));

			this.IsAttentionRequired = false;
			this.Title = title ?? throw new ArgumentNullException(nameof(title));
			this.MetricUnit = metricUnit ?? throw new ArgumentNullException(nameof(metricUnit));
			this.MetricValue = _defaultMetricValue;
			this.Details = _defaultDetails;
		}

		public string Title { get; }
		public string MetricValue { get; protected set;}
		public string MetricUnit { get; }
		public string Details { get; protected set; }
		public bool IsAttentionRequired { get; protected set; }
		public void Refresh(ImmutableArray<IRecord> records)
		{
			this.IsAttentionRequired = false;
			this.MetricValue = _defaultMetricValue;
			this.Details = _defaultDetails;

			OnRefresh(records);
		}

		protected abstract void OnRefresh(ImmutableArray<IRecord> records);
	}
}
