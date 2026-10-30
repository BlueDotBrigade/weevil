namespace BlueDotBrigade.Weevil.Configuration.Sidecar
{
	using Data;

	internal interface ISidecarLoader
	{
		bool Load();

		void Save(bool deleteBackup, RecordList allRecords);
	}
}