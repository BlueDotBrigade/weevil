namespace BlueDotBrigade.Weevil.Configuration.Software
{
	using System;

	public class ApplicationInfo
	{
		public static readonly ApplicationInfo NotSpecified = new ApplicationInfo();

		public Version Version { get; set; }
		public string Description { get; set; }
		public string InstallerPath { get; set; }
		public string ChangeLogPath { get; set; }
	}
}