namespace BlueDotBrigade.Weevil
{
	using System;
	using System.Collections.Generic;
	using BlueDotBrigade.Weevil.Analysis;

	public interface IPlugin
	{
		string Name { get; }

		/// <remarks>
		/// May be called for many logs, and on several threads at once, on the same instance: the Open As dialog asks as the analyst selects logs (#1216).
		/// So keep no state between calls, and read only what is needed to tell, since the analyst may select any file.
		/// For a log in a zip, the dialog gives a temporary file of the same name that holds only the log's first 64 KB, with nothing beside it:
		/// tell a log by its start, not by its length, its last line or the files beside it.
		/// </remarks>
		bool CheckCompatibility(string sourceFilePath);

		bool CanShowDashboard { get; }

		/// <remarks>
		/// Weevil does not close a plugin's dashboard when another log opens, so <paramref name="engine"/> can outlive its log.
		/// In disk mode, a read through it then throws <see cref="IO.LogFileChangedException"/> when the log changed,
		/// or when the copy of a zipped log was deleted (#1025, #1139, #1295).
		/// Weevil disposes <paramref name="engine"/> once another log opens, which releases the log's lock (#1254): do not write through it, for example a comment or a save, after that.
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