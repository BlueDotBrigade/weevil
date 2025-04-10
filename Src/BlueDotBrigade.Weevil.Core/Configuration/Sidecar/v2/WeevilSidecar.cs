﻿namespace BlueDotBrigade.Weevil.Configuration.Sidecar.v2
{
	using System.Runtime.Serialization;

	public class WeevilSidecar
	{
		public WeevilSidecar()
		{
			this.Header = new Header();
			this.CommonData = new CommonData();
		}

		/// <summary>
		/// Contains information that is needed in order to save & load sidecar metadata.
		/// </summary>
		[DataMember]
		public Header Header { get; set; }

		/// <summary>
		/// Contains data that is plugin specific.
		/// </summary>
		[DataMember]
		public PluginData PluginData { get; set; }

		/// <summary>
		/// Contains data that is common to all plugins.
		/// </summary>
		[DataMember]
		public CommonData CommonData { get; set; }
	}
}
