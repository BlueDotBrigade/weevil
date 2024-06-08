namespace BlueDotBrigade.Weevil.Cli.IO
{
	using System.Diagnostics;
	using System;

	internal class ConsoleWriter : IOutputWriter
	{
		public void WriteLine(string message)
		{
			Console.WriteLine(message);
		}
	}
}