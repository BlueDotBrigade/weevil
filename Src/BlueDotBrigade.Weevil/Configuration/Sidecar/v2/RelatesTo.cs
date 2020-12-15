namespace BlueDotBrigade.Weevil.Configuration.Sidecar.v2
{
	public class RelatesTo
	{
		public RelatesTo()
		{
			this.LineNumber = -1;
			this.ByteOffset = -1;
		}
		public int LineNumber { get; set; }
		public long ByteOffset { get; set; }
	}
}