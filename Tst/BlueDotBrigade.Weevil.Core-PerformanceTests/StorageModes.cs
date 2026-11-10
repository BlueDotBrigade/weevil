namespace BlueDotBrigade.Weevil
{
	using StorageMode = BlueDotBrigade.Weevil.IO.StorageMode;

	/// <summary>
	/// Opens logs in a chosen storage mode.
	/// </summary>
	internal static class StorageModes
	{
		/// <summary>
		/// Returns a builder that opens the log in the given storage mode.
		/// </summary>
		/// <remarks>
		/// Until the storage mode is public (#1028), only a Debug build can choose it, through Core's internal builder.
		/// A Release build, as on the reference machine, opens in RAM, and cannot run the tests on disk.
		/// </remarks>
		public static IEngineBuilder UsingPath(string logPath, StorageMode storageMode)
		{
#if DEBUG
			return new EngineBuilder(logPath).UsingStorage(storageMode);
#else
			if (storageMode != StorageMode.Ram)
			{
				Assert.Inconclusive("Only a Debug build can open a log on disk, until the storage mode is public (#1028).");
			}

			return Engine.UsingPath(logPath);
#endif
		}
	}
}
