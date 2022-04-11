namespace BlueDotBrigade.Weevil.Gui.Diagnostics
{
	using System.Diagnostics;
	using System.Linq;

	internal static class PerformanceCounterHelper
	{
		/// <summary>
		/// Retrieves the <see cref="PerformanceCounter"/> instance name for the given <paramref name="process"/>.
		/// </summary>
		/// <param name="process">The desired process.</param>
		/// <returns>
		/// Returns an instance name.
		/// </returns>
		/// <remarks>
		/// .NET Framework 4.8 requires that you use an instance name, instead of a process identifier (PID),
		/// to retrieve <see cref="PerformanceCounter"/> values.
		/// </remarks>
		/// <seealso cref="PerformanceCounter"/>
		public static string GetInstanceName(Process process)
		{
			var result = string.Empty;

			var category = new PerformanceCounterCategory("Process");
			var instanceNames = category
				.GetInstanceNames()
				.Where(x => x.Contains(process.ProcessName));

			foreach (var instanceName in instanceNames)
			{
				using (var performanceCounter = new PerformanceCounter("Process", "ID Process", instanceName, true))
				{
					if (performanceCounter.RawValue == process.Id)
					{
						result = instanceName;
						break;
					}
				}
			}

			return result;
		}
	}
}
