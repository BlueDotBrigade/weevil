namespace BlueDotBrigade.Weevil.IO
{
	using System;

	public class ConsoleWriter : IOutputWriter
	{
		public void WriteLine(string message)
		{
			Console.WriteLine(message);
		}
	}
}
