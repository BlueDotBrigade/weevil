namespace BlueDotBrigade.Weevil.Configuration
{
	using System;
	using System.Diagnostics;
	using System.Runtime.Serialization;

	/// <summary>
	/// Message is used to inform the user when a new version of the application has been released.
	/// </summary>
	/// <remarks>
	/// <see cref="DataContractSerializer "/> requires that the XML elements be organized alphabetically.
	/// Failure to do so will result in <see langword="null" /> values.
	/// </remarks>
	/// <seealso href="https://stackoverflow.com/a/14015198/949681">StackOverflow: DataContractSerializer fails, null data</seealso>
	[Serializable]
	[DataContract(Namespace = "")]
	[DebuggerDisplay("Version={" + nameof(LatestReleaseVersion) + ("}, CodeName={" + nameof(CodeName) + "}"))]
	public class ApplicationInfo
	{
		public static readonly ApplicationInfo NotSpecified = new ApplicationInfo
		{
			ChangeLogUrl = string.Empty,
			CodeName = string.Empty,
			Description = string.Empty,
			InstallerUrl = string.Empty,
			LatestRelease = "0.0.0.0",
		};

		[DataMember]
		public string ChangeLogUrl { get; set; }

		[DataMember]
		public string CodeName { get; set; }

		[DataMember]
		public string Description { get; set; }

		[DataMember]
		public string InstallerUrl { get; set; }

		[DataMember]
		public string LatestRelease { get; set; }

		[IgnoreDataMember]
		public Version LatestReleaseVersion => Version.Parse(this.LatestRelease);
	}
}