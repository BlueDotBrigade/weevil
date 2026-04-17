namespace BlueDotBrigade.Weevil.Cli.IO
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using BlueDotBrigade.Weevil.IO;

	public static class Write
	{
		public static void Text(string message) => OutputWriterContext.WriteText(message);
		public static void Heading(string message) => OutputWriterContext.WriteHeading(message);
		public static void Bullet(string message) => OutputWriterContext.WriteBullet(message);
		public static void Numbered(string message) => OutputWriterContext.WriteNumbered(message);
		public static void Error(string message) => OutputWriterContext.WriteError(message);
	}
}
