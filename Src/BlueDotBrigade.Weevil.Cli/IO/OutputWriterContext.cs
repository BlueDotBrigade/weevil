namespace BlueDotBrigade.Weevil.Cli.IO
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.IO;

	public static class OutputWriterContext
	{
		private static IOutputFormatter _outputFormatter = new PlainTextFormatter();
		private static IOutputWriter _outputWriter = new DebugWriter();

		/// <seealso href="https://refactoring.guru/design-patterns/strategy">Refactoring Guru: Strategy Pattern</seealso>
		public static void Configure(IOutputFormatter formatter, IOutputWriter writer)
		{
			_outputFormatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
			_outputWriter = writer ?? throw new ArgumentNullException(nameof(writer));
			_outputFormatter.ResetNumbering();
		}

		public static void WriteText(string message)
		{
			_outputFormatter.ResetNumbering();
			_outputWriter.WriteLine(_outputFormatter.AsText(message));
		}

		public static void WriteHeading(string message)
		{
			_outputFormatter.ResetNumbering();
			_outputWriter.WriteLine(_outputFormatter.AsHeading(message));
		}

		public static void WriteBullet(string message)
		{
			_outputFormatter.ResetNumbering();
			_outputWriter.WriteLine(_outputFormatter.AsBullet(message));
		}

		public static void WriteNumbered(string message)
		{
			_outputWriter.WriteLine(_outputFormatter.AsNumbered(message));
		}

		public static void WriteError(string message)
		{
			_outputFormatter.ResetNumbering();
			_outputWriter.WriteLine(_outputFormatter.AsError(message));
		}
	}
}
