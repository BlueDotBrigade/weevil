namespace BlueDotBrigade.Weevil.Gui.Management
{
	using System;
	using System.Management;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.IO;

	class ComputerSnapshot
	{
		private const string DefaultString = "Unknown";
		//private const uint DefaultInt = -1;

		public ComputerSnapshot()
		{
			this.CpuName = DefaultString;
			this.CpuProcessors = 0;
			this.CpuCores = 0;
			this.OperatingSystem = DefaultString;
			this.CpuName = DefaultString;
			this.CpuName = DefaultString;
			this.RamTotal = new StorageUnit();
		}

		public string CpuName { get; private set; }


		public uint CpuProcessors { get; private set; }
		public uint CpuCores { get; private set; }

		public string OperatingSystem { get; private set; }

		public StorageUnit RamTotal { get; private set; }

		public uint RamSlotsUsed { get; private set;}

		public override string ToString()
		{
			return
				$"{nameof(OperatingSystem)}=`{this.OperatingSystem}`, " +
				$"{nameof(CpuName)}=`{this.CpuName}`, " +
				$"{nameof(CpuProcessors)}={this.CpuProcessors}, " +
				$"{nameof(CpuCores)}={this.CpuCores}, " +
				$"{nameof(this.RamTotal)}={this.RamTotal.GigaBytes:0.00}GB";
		}

		public static ComputerSnapshot Create()
		{
			var snapshot = new ComputerSnapshot();

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
				var ramQuery = new SelectQuery("select * from Win32_PhysicalMemory");

				using (var searcher = new ManagementObjectSearcher(ramQuery))
				{
					using (ManagementObjectCollection memorySticks = searcher.Get())
					{
						snapshot.RamSlotsUsed = (uint)memorySticks.Count;

						foreach (ManagementBaseObject memoryStick in memorySticks)
						{
							var managementObject = (ManagementObject)memoryStick;

							var capacity = (ulong)managementObject["Capacity"];
							snapshot.RamTotal += new StorageUnit(capacity);
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
			snapshot.OperatingSystem = Environment.OSVersion.ToString();

			return snapshot;
		}
	}
}
