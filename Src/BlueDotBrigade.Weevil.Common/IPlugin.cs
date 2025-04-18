﻿namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Generic;
	using BlueDotBrigade.Weevil.Analysis;
	using IO;

	public interface IPlugin
	{
		string Name { get; }

		bool CheckCompatibility(string sourceFilePath);

		bool CanOpenAs { get; }

		(bool, OpenAsResult) ShowOpenAs(string licensePath, CreateEngineBuilder createEngineBuilder, string sourceFilePath);

		bool CanShowDashboard { get; }

		void ShowDashboard(Version weevilVersion, IEngine engine, IInsight[] insights);

		ICoreExtension GetExtension(string sourceFilePath);

		IList<string> GetDefaultInclusiveHistory();
		IList<string> GetDefaultExclusiveHistory();
	}
}