namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	internal class FileRemarksChangedBulletin
	{
		public bool HasFileRemarks { get; }

		public FileRemarksChangedBulletin(bool hasFileRemarks)
		{
			this.HasFileRemarks = hasFileRemarks;
		}
	}
}
