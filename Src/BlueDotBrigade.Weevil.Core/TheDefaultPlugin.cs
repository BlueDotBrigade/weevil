namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Generic;
	using BlueDotBrigade.Weevil.Analysis;
	using IO;

	internal class TheDefaultPlugin : IPlugin
	{
		/// <inheritdoc />
		public string Name => GetType().Assembly.FullName;

		/// <inheritdoc />
		public bool CheckCompatibility(string sourceFilePath)
		{
			return true;
		}

		/// <inheritdoc />
		public bool CanOpenAs => false;

		public (bool, OpenAsResult) ShowOpenAs(object parentWindow, string license, CreateEngineBuilder createEngineBuilder, string sourceFilePath)
		{
			throw new NotSupportedException();
		}

		public bool CanShowDashboard => false;

		public void ShowDashboard(object parentWindow, IEngine engine, IInsight[] insights)
		{
			throw new NotImplementedException();
		}

		public ICoreExtension GetExtension(string sourceFilePath)
		{
			return new DefaultCoreExtension();
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
