namespace BlueDotBrigade.Weevil.Gui.Management
{
	using System;
	using System.Management;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.IO;

	class ComputerSnapshot
	{
		private const string DefaultString = "Unknown";

		public ComputerSnapshot()
		{
			this.CpuName = DefaultString;
			this.CpuProcessors = 0;
			this.CpuCores = 0;
			this.OsName = DefaultString;
			this.CpuName = DefaultString;
			this.CpuName = DefaultString;
			this.RamTotalInstalled = new StorageUnit();
		}

		public string CpuName { get; private set; }

		public uint CpuProcessors { get; private set; }
		public uint CpuCores { get; private set; }

		public string OsName { get; private set; }
		public string OsVersion { get; private set; }
		public bool OsIs64Bit { get; private set; }
		public DateTime OsInstalledAt { get; private set; }
		public DateTime OsBootedAt { get; private set; }

		/// <summary>
		/// Total amount of physical memory available to the operating system.
		/// This value does not necessarily indicate the true amount of physical memory,
		/// but what is reported to the operating system as available to it.
		/// </summary>
		public StorageUnit RamTotalInstalled { get; private set; }

		/// <summary>
		/// Represents the amount of physical memory that is not currently in use.
		/// </summary>
		public StorageUnit RamTotalFree { get; private set; }

		public uint RamSlotsUsed { get; private set;}

		public static ComputerSnapshot Create()
		{
			var snapshot = new ComputerSnapshot
			{
				OsName = Environment.OSVersion.ToString(),
				OsIs64Bit = Environment.Is64BitOperatingSystem,
			};
			
			try
			{
				var cpuQuery = new SelectQuery("SELECT * FROM Win32_Processor");

				using (var searcher = new ManagementObjectSearcher(cpuQuery))
				{
					using (ManagementObjectCollection results = searcher.Get())
					{
						foreach (ManagementBaseObject result in results)
						{
							var managementObject = (ManagementObject)result;

							snapshot.CpuName = managementObject["Name"] as string;
							snapshot.CpuProcessors = (uint)managementObject["NumberOfLogicalProcessors"];
							snapshot.CpuCores = (uint)managementObject["NumberOfCores"];
							break;
						}
					}
				}

				var osQuery = new SelectQuery("select * from Win32_OperatingSystem");

				using (var searcher = new ManagementObjectSearcher(osQuery))
				{
					using (ManagementObjectCollection results = searcher.Get())
					{
						foreach (ManagementBaseObject result in results)
						{
							var managementObject = (ManagementObject)result;

							snapshot.OsName = managementObject["Caption"] as string;
							snapshot.OsVersion = managementObject["Version"] as string;

							var installDate = managementObject["InstallDate"].ToString();
							snapshot.OsInstalledAt = ManagementDateTimeConverter.ToDateTime(installDate);

							var bootedAt = managementObject["LastBootUpTime"].ToString();
							snapshot.OsBootedAt = ManagementDateTimeConverter.ToDateTime(bootedAt);

							var totalVisibleKb = (ulong)managementObject["TotalVisibleMemorySize"];
							snapshot.RamTotalInstalled += new StorageUnit(totalVisibleKb * StorageUnit.BytesPerKilobyte);

							var totalFreeKb = (ulong)managementObject["FreePhysicalMemory"];
							snapshot.RamTotalFree += new StorageUnit(totalFreeKb * StorageUnit.BytesPerKilobyte);

							break;
						}
					}
				}
			}
			catch (Exception e)
			{
				Log.Default.Write(
					LogSeverityType.Error,
					e,
					"An unexpected error occurred while taking a snapshot of the computer hardware.");
			}

			return snapshot;
		}
	}
}
