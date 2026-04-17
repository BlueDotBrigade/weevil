namespace BlueDotBrigade.Weevil.IO
{
	using System.Diagnostics;

	internal class DebugWriter : IOutputWriter
	{
		public void WriteLine(string message)
		{
			Debug.WriteLine(message);
		}
	}
}
