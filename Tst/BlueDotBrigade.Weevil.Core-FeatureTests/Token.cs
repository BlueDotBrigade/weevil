namespace BlueDotBrigade.Weevil
{
	using System.Security;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Filter;
	using NSubstitute.Extensions;

	/// <remarks>
	/// Reqnroll disposes it when the scenario ends, which closes the open log, so the next scenario can open it (#1254).
	/// </remarks>
	internal sealed class Token : IDisposable
	{
		private IEngine _engine;

		public Token()
		{
			_engine = BlueDotBrigade.Weevil.Engine.Surrogate;

			this.IncludeFilter = string.Empty;
			this.ExcludeFilter = string.Empty;

			this.FilterType = FilterType.RegularExpression;

			this.Configuration = new Dictionary<string, object>()
			{
				{ "IncludePinned", false },
				{ "IncludeBookmarks", false },
				{ "IsCaseSensitive", true },
				{ "HideDebugRecords", false },
				{ "HideTraceRecords", false },
			};
		}

		/// <summary>
		/// The open log. Setting another closes the one it replaces, as opening a log in Weevil's window does (#1254).
		/// </summary>
		public IEngine Engine
		{
			get => _engine;
			set
			{
				IEngine replaced = _engine;
				_engine = value;

				if (!ReferenceEquals(replaced, value))
				{
					replaced.Dispose();
				}
			}
		}

		public string IncludeFilter { private get; set; }

		public string ExcludeFilter { private get; set; }

		public FilterType FilterType { get; set; }

		public Dictionary<string, object> Configuration { get; set; }

		public Analysis.IInsight LastInsight { get; set; }

		public FilterCriteria FilterCriteria => new FilterCriteria(
			this.IncludeFilter, 
			this.ExcludeFilter, 
			this.Configuration);

		public RecordList Results => this.Engine.Filter.Results;

		/// <summary>
		/// Closes the open log.
		/// </summary>
		public void Dispose()
		{
			_engine.Dispose();
		}
	}
}
