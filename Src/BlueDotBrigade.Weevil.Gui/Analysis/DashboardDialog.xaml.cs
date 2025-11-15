namespace BlueDotBrigade.Weevil.Gui.Analysis
{
	using System;
	using System.Collections.Immutable;
	using System.Windows;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Data;

	/// <summary>
	/// Interaction logic for DashboardDialog.xaml
	/// </summary>
	public partial class DashboardDialog : Window
	{
		public static readonly DependencyProperty WeevilVersionProperty =
			DependencyProperty.Register(
				nameof(WeevilVersion), typeof(Version),
				typeof(DashboardDialog));

		public static readonly DependencyProperty InsightsProperty =
			DependencyProperty.Register(
				nameof(Insights), typeof(IInsight[]),
				typeof(DashboardDialog));

		public static readonly DependencyProperty FromProperty =
			DependencyProperty.Register(
				nameof(From), typeof(DateTime),
				typeof(DashboardDialog));

		public static readonly DependencyProperty ToProperty =
			DependencyProperty.Register(
				nameof(To), typeof(DateTime),
				typeof(DashboardDialog));

		public static readonly DependencyProperty ContextProperty =
			DependencyProperty.Register(
				nameof(Context), typeof(ContextDictionary),
				typeof(DashboardDialog));

		public static readonly DependencyProperty SourceFilePathProperty =
			DependencyProperty.Register(
				nameof(SourceFilePath), typeof(string),
				typeof(DashboardDialog));

		private readonly IEngine _engine;
		private readonly IBulletinMediator _bulletinMediator;

		public Version WeevilVersion
		{
			get => (Version)GetValue(WeevilVersionProperty);
			set => SetValue(WeevilVersionProperty, value);
		}

		public IInsight[] Insights
		{
			get => (IInsight[])GetValue(InsightsProperty);
			set => SetValue(InsightsProperty, value);
		}

		public DateTime From
		{
			get => (DateTime)GetValue(FromProperty);
			set => SetValue(FromProperty, value);
		}

		public DateTime To
		{
			get => (DateTime)GetValue(ToProperty);
			set => SetValue(ToProperty, value);
		}

		public string SourceFilePath
		{
			get => (string)GetValue(SourceFilePathProperty);
			set => SetValue(SourceFilePathProperty, value);
		}

		public ContextDictionary Context
		{
			get => (ContextDictionary)GetValue(ContextProperty);
			set => SetValue(ContextProperty, value);
		}

		public DashboardDialog(Version weevilVersion, IEngine engine, IBulletinMediator bulletinMediator)
		{
			_engine = engine ?? throw new ArgumentNullException(nameof(engine));
			_bulletinMediator = bulletinMediator ?? throw new ArgumentNullException(nameof(bulletinMediator));

			InitializeComponent();

			this.WeevilVersion = weevilVersion;

			this.DataContext = this;

			this.Context = engine.Context;
			this.SourceFilePath = engine.SourceFilePath;

			var range = _engine.Records.GetEstimatedRange();
			this.From = range.From;
			this.To = range.To;
		}

		private void OnRefresh(object sender, RoutedEventArgs e)
		{
			foreach (IInsight insight in this.Insights)
			{
				insight.Refresh(_engine.Filter.Results);
			}

			var range = _engine.Filter.Results.GetEstimatedRange();
			this.From = range.From;
			this.To = range.To;
		}

		private void OnCopy(object sender, RoutedEventArgs e)
		{
			var report = new InsightReportGenerator().Generate(
				this.WeevilVersion,
				_engine,
				this.Insights.ToImmutableArray(),
				this.From,
				this.To);
			Clipboard.SetData(DataFormats.UnicodeText, report);
		}

		private void OnNavigateToRecord(object sender, RoutedEventArgs e)
		{
			if (sender is FrameworkElement element && element.DataContext is IInsight insight)
			{
				if (insight.RelatedRecords.Length > 0)
				{
					_bulletinMediator.Post(new NavigateToInsightRecordBulletin(insight.RelatedRecords));
					
					// Bring the main window to the front
					if (this.Owner != null)
					{
						this.Owner.Activate();
					}
				}
			}
		}
	}
}
