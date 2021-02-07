namespace BlueDotBrigade.Weevil.Configuration
{
	using System;
	using System.Diagnostics;
	using System.Runtime.Serialization;

	/// <summary>
	/// Message is used to inform the user when a new version of the application has been released.
	/// </summary>
	/// <remarks>
	/// In order for deserialization to work properly, the <see cref="ApplicationInfo"/> properties must be in the same order
	/// as they appear in the XML.
	/// </remarks>
	[Serializable]
	[DataContract(Namespace = "")]
	[DebuggerDisplay("Version={" + nameof(Version) + "}")]
	public class ApplicationInfo
	{
		public static readonly ApplicationInfo NotSpecified = new ApplicationInfo();

		[DataMember]
		public string InstallerUrl { get; set; }

		[DataMember]
		public string ChangeLogUrl { get; set; }


		[DataMember]
		public string Description { get; set; }

		[DataMember]
		public Version Version { get; set; }
	}
}