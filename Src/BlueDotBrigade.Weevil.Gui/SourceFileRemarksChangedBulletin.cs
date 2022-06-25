namespace BlueDotBrigade.Weevil.Gui
{
	internal class SourceFileRemarksChangedBulletin
	{
		public bool HasSourceFileRemarks { get; }

		public SourceFileRemarksChangedBulletin(bool hasSourceFileRemarks)
		{
			this.HasSourceFileRemarks = hasSourceFileRemarks;
		}
	}
}
