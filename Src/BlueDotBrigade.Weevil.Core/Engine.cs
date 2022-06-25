namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Immutable;
	using System.Diagnostics;
	using System.Diagnostics.CodeAnalysis;
	using System.IO;
	using Analysis;
	using Data;
	using Filter;
	using Navigation;
	using Reports;

	[DebuggerDisplay("InstanceId={_coreEngine.InstanceId}, Records={_coreEngine.Count}, Context={_coreEngine.Context.ToString()}")]
	public class Engine : IEngine
	{
		public static readonly IEngine Surrogate;

		private CoreEngine _coreEngine;

		internal Engine(CoreEngine coreEngine)
		{
			_coreEngine = coreEngine;
		}

		[SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline", Justification = "Method required to build complex instance.")]
		static Engine()
		{
			var directoryPath = Path.Combine(
				Environment.GetEnvironmentVariable("LocalAppData"),
				"Weevil");

			Directory.CreateDirectory(directoryPath);

			var filePath = Path.Combine(directoryPath, "EmptyFile.log");

			if (!File.Exists(filePath))
			{
				File.WriteAllText(filePath, string.Empty);
			}

			Surrogate = UsingPath(filePath).Open();
		}


		public static bool IsRealInstance(IEngine instance)
		{
			var isSurrogate = instance?.Equals(Surrogate) ?? false;

			return !isSurrogate;
		}

		public IRecord this[int index] => _coreEngine[index];

		public ImmutableArray<IRecord> Records => _coreEngine.Records;

		public LogFileMetrics Metrics => _coreEngine.Metrics;

		public IFilter Filter => _coreEngine.Filter;
		public INavigate Navigate => _coreEngine.Navigate;
		public ISelect Selector => _coreEngine.Selector;
		public IAnalyze Analyzer => _coreEngine.Analyzer;

		public int Count => _coreEngine.Count;

		public ContextDictionary Context => _coreEngine.Context;

		public string UserRemarks
		{
			get
			{
				return _coreEngine.UserRemarks;
			}
			set
			{
				_coreEngine.UserRemarks = value;
			}
		}

		public string SourceFilePath => Engine.IsRealInstance(this)
			? _coreEngine.SourceFilePath
			: string.Empty;

		public string SourceDirectory => Engine.IsRealInstance(this)
			? _coreEngine.SourceDirectory
			: string.Empty;

		public bool HasBeenCleared => _coreEngine.HasBeenCleared;

		public void Save()
		{
			_coreEngine.Save();
		}

		public void Save(bool deleteSidecarBackup)
		{
			_coreEngine.Save(deleteSidecarBackup);
		}

		public static IEngineBuilder UsingPath(string sourceFilePath)
		{
			return new EngineBuilder(sourceFilePath);
		}

		public static IEngineBuilder UsingPath(string sourceFilePath, int lineNumber)
		{
			return new EngineBuilder(sourceFilePath, lineNumber);
		}

		public void Clear(ClearOperation clearOperation)
		{
			_coreEngine = _coreEngine.FromInstance(clearOperation).Build();
		}

		public void Reload()
		{
			var sourcePath = _coreEngine.SourceFilePath;
			FilterType filterType = _coreEngine.Filter.FilterType;
			IFilterCriteria filterCriteria = _coreEngine.Filter.Criteria;

			_coreEngine = CoreEngine.FromPath(sourcePath).Build();
			_coreEngine.Filter.Apply(filterType, filterCriteria);
		}

		public void GenerateReport(ReportType report, string destinationFolder)
		{
			_coreEngine.GenerateReport(report, destinationFolder);
		}
	}
}
