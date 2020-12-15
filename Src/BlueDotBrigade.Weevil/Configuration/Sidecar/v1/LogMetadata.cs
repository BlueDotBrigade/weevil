namespace BlueDotBrigade.Weevil.Configuration.Sidecar.v1
{
	using System.Collections.Generic;

	public class LogMetadata
	{
		public LogMetadata()
		{
			this.Labels = new List<Label>();
		}

		public List<Label> Labels { get; }
	}
}
