using System.Diagnostics;

namespace BlueDotBrigade.Weevil.IO
{
	internal class DebugWriter : IOutputWriter
	{
		public void WriteLine(string message)
		{
			Debug.WriteLine(message);
		}
	}
}
