namespace BlueDotBrigade.Weevil.Cli.IO
{
	using System.Diagnostics;
	using System;

	internal class DebugWriter : IOutputWriter
	{
		public void WriteLine(string message)
		{
			Debug.WriteLine(message);
		}
	}
}
