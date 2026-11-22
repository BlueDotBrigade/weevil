namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using BlueDotBrigade.Weevil.Analysis;
	using BlueDotBrigade.Weevil.Data;
	using BlueDotBrigade.Weevil.Diagnostics;
	using IO;

	internal class TsvPlugin : IPlugin
	{
		/// <inheritdoc />
		public string Name => GetType().Assembly.FullName;

		/// <inheritdoc />
		public bool CheckCompatibility(string sourceFilePath)
		{
			var isCompatible = false;

			var coreExtension = new TsvCoreExtension();

			using (FileStream log = FileHelper.Open(sourceFilePath))
			{
				var streamReader = new StreamReader(log);
				IRecordParser recordParser = coreExtension.GetRecordParser();

				var content = streamReader.ReadLine();
				if (recordParser.TryParse(0, content, out IRecord record))
				{
					isCompatible = true;
					// At Debug: the Open As dialog asks as each log is selected (#1216), and PluginFactory says which plugin it chose.
					Log.Default.Write(LogSeverityType.Debug, $"A compatible core extension has been found. CoreExtension={coreExtension.Name}, SourceFilePath={sourceFilePath}");
				}
			}

			return isCompatible;
		}

		public bool CanShowDashboard => false;

		public void ShowDashboard(Version weevilVersion, IEngine engine, IInsight[] insights)
		{
			throw new NotImplementedException();
		}

		public ICoreExtension GetExtension(string sourceFilePath)
		{
			return new TsvCoreExtension();
		}

		public IList<string> GetDefaultInclusiveHistory()
		{
			return new List<string>();
		}

		public IList<string> GetDefaultExclusiveHistory()
		{
			return new List<string>();
		}
	}
}