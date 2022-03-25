namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using BlueDotBrigade.Weevil.Configuration;

	internal class SoftwareDetailsBulletin
	{
		public SoftwareDetailsBulletin()
		{
			this.CurrentVersion = ApplicationInfo.NotSpecified.LatestReleaseVersion;
			this.LatestReleaseDetails = ApplicationInfo.NotSpecified;
			this.IsUpdateAvailable = false;
		}

		public SoftwareDetailsBulletin(Version currentVersion)
		{
			this.CurrentVersion = currentVersion;
			this.LatestReleaseDetails = ApplicationInfo.NotSpecified;
			this.IsUpdateAvailable = false;
		}

		public SoftwareDetailsBulletin(Version currentVersion, ApplicationInfo latestReleaseDetails)
		{
			this.CurrentVersion = currentVersion;
			this.LatestReleaseDetails = latestReleaseDetails ?? ApplicationInfo.NotSpecified;

			if (this.LatestReleaseDetails != null)
			{
				if (!this.LatestReleaseDetails.Equals(ApplicationInfo.NotSpecified))
				{
					var current = new Version(
						currentVersion.Major, 
						currentVersion.Minor, 
						currentVersion.Build);
					
					var latest = new Version(
						latestReleaseDetails.LatestReleaseVersion.Major, 
						latestReleaseDetails.LatestReleaseVersion.Minor, 
						latestReleaseDetails.LatestReleaseVersion.Build);

					this.IsUpdateAvailable = latest > current;
				}
			}
		}
		public bool IsUpdateAvailable { get; }

		public ApplicationInfo LatestReleaseDetails { get; }

		public Version CurrentVersion { get; }
	}
}
