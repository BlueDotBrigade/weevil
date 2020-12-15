namespace BlueDotBrigade.Weevil.Gui
{
	using System;

	internal class DroppedFileEventArgs
	{
		public DroppedFileEventArgs(string filePath)
		{
			this.FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
		}

		public string FilePath { get; private set; }
	}
}