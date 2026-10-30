namespace BlueDotBrigade.Weevil.Configuration.Sidecar
{
	using System.Collections.Immutable;
	using Data;

	internal interface ISidecarLoader
	{
		bool Load();

		void Save(bool deleteBackup, RecordList allRecords);
	}
}