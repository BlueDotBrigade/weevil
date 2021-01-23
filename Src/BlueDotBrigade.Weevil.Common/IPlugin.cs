namespace BlueDotBrigade.Weevil
{
	using System.Collections.Generic;
	using IO;

	public interface IPlugin
	{
		string Name { get; }

		bool CheckCompatibility(string sourceFilePath);

		bool CanOpenAs { get; }

		(bool, OpenAsResult) ShowOpenAs(object parentWindow, CreateEngineBuilder createEngineBuilder, string sourceFilePath);

		ICoreExtension GetExtension(string sourceFilePath);

		IList<string> GetDefaultInclusiveHistory();
		IList<string> GetDefaultExclusiveHistory();
	}
}
