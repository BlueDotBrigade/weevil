namespace BlueDotBrigade.Weevil.Cli.IO
{
	using BlueDotBrigade.Weevil.IO;

	public static class Write
	{
		public static void Text(string message) => OutputWriterContext.WriteText(message);
		public static void Heading(string message) => OutputWriterContext.WriteHeading(message);
		public static void SubHeading(string message) => OutputWriterContext.WriteSubHeading(message);
		public static void Bullet(string message) => OutputWriterContext.WriteBullet(message);
		public static void Numbered(string message) => OutputWriterContext.WriteNumbered(message);
		public static void Error(string message) => OutputWriterContext.WriteError(message);
		public static void TableHeader(string[] headers) => OutputWriterContext.WriteTableHeader(headers);
		public static void TableRow(string[] columns) => OutputWriterContext.WriteTableRow(columns);
		public static void Table(string[] headers, string[][] rows) => OutputWriterContext.WriteTable(headers, rows);
	}
}
