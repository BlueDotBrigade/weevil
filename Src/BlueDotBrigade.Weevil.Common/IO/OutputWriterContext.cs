namespace BlueDotBrigade.Weevil.IO
{
	using System;

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

		public static void WriteSubHeading(string message)
		{
			_outputFormatter.ResetNumbering();
			_outputWriter.WriteLine(_outputFormatter.AsSubHeading(message));
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

		public static void WriteWarning(string message)
		{
			_outputFormatter.ResetNumbering();
			_outputWriter.WriteLine(_outputFormatter.AsWarning(message));
		}

		public static void WriteTableHeader(string[] headers)
		{
			_outputFormatter.ResetNumbering();
			_outputWriter.WriteLine(_outputFormatter.AsTableHeader(headers));
		}

		public static void WriteTableRow(string[] columns)
		{
			_outputWriter.WriteLine(_outputFormatter.AsTableRow(columns));
		}

		public static void WriteTable(string[] headers, string[][] rows)
		{
			_outputFormatter.ResetNumbering();
			_outputWriter.WriteLine(_outputFormatter.AsTable(headers, rows));
		}
	}
}
