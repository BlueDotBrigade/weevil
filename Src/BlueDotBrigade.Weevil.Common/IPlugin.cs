namespace BlueDotBrigade.Weevil
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

		/// <remarks>
		/// Weevil does not close a plugin's dashboard when another log opens, so <paramref name="engine"/> can outlive its log.
		/// In disk mode, a read through it then throws an <see cref="System.IO.IOException"/>, such as <see cref="IO.LogFileChangedException"/>
		/// when the log changed, or <see cref="System.IO.FileNotFoundException"/> when the copy of a zipped log was deleted (#1025, #1139).
		/// </remarks>
		void ShowDashboard(Version weevilVersion, IEngine engine, IInsight[] insights);

		/// <remarks>
		/// Which extension, and so which record parser, is returned must depend only on the log's content, not on its path:
		/// a log reopened from its saved index, even after it was renamed, keeps the records the parser made (#1023). See <see cref="Data.IRecordParser"/>.
		/// </remarks>
		ICoreExtension GetExtension(string sourceFilePath);

		IList<string> GetDefaultInclusiveHistory();
		IList<string> GetDefaultExclusiveHistory();
	}
}