namespace BlueDotBrigade.Weevil.Gui.Diagnostics
{
	using System;
	using System.Diagnostics;
	using BlueDotBrigade.Weevil.IO;

	internal static class TaskManager
	{
		/// <summary>
		/// Determines the amount of physical memory (RAM) that is being used by the given <paramref name="process"/>.
		/// </summary>
		/// <param name="process">Represents the process that will be analyzed.</param>
		/// <returns>
		/// Returns the amount of RAM being used by the <paramref name="process"/>.
		/// </returns>
		/// <remarks>
		/// While the Task Manager is often used to determine how much RAM an application has been allocated,
		/// a number of articles indicate that this value can be off by more than 100MB from the true value.
		///
		/// Similar to Microsoft's Process explorer, consider referencing the following values instead:
		/// <list type="bullet">
		///		<item>Process.WorkingSet64</item>
		///		<item>Process.PrivateMemorySize64</item>
		/// </list>
		/// </remarks>
		/// <seealso href="https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.workingset64">Process.WorkingSet64</seealso>
		/// <seealso href="https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.process.privatememorysize64">Process.PrivateMemorySize64</seealso>
		/// <seealso href="https://docs.microsoft.com/en-us/sysinternals/downloads/process-explorer">Microsoft Process Explorer</seealso>
		[Obsolete("Consider referencing a more reliable source like System.Diagnostics.Process.WorkingSet64.")]
		public static StorageUnit GetPrivateWorkingSet(Process process)
		{
			StorageUnit privateWorkingSet;

			var nameToUseForMemory = PerformanceCounterHelper.GetInstanceName(process);

			// The only way to retrieve the same `private working set` as the Task Manager
			// is to use the following Performance Counter.  The following will not return the same value:
			// ... System.Diagnostics.Process properties
			// ... WMI's Win32_PerfRawData_PerfProc_Process
			using (var procPerfCounter = new PerformanceCounter("Process", "Working Set - Private", nameToUseForMemory))
			{
				// This RawValue is measured in bytes.
				privateWorkingSet = new StorageUnit(procPerfCounter.RawValue); 
			}
			return privateWorkingSet;
		}
	}
}
