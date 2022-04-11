namespace BlueDotBrigade.Weevil.Gui.Diagnostics
{
	using System.Diagnostics;
	using BlueDotBrigade.Weevil.IO;

	internal static class TaskManager
	{
		/// <summary>
		/// Determines the amount of physical memory (RAM) that is being used by the given <paramref name="process"/>.
		/// </summary>
		/// <param name="process">Represents the process that will be analyzed.</param>
		/// <returns>
		/// </returns>
		public static StorageUnit GetPrivateWorkingSet(Process process)
		{
			StorageUnit privateWorkingSet;

			var nameToUseForMemory = PerformanceCounterHelper.GetInstanceName(process);
			using (var procPerfCounter = new PerformanceCounter("Process", "Working Set - Private", nameToUseForMemory))
			{
				// This RawValue is measured in bytes.
				privateWorkingSet = new StorageUnit(procPerfCounter.RawValue); 
			}
			return privateWorkingSet;
		}
	}
}
