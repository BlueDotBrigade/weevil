namespace BlueDotBrigade.Weevil.IO
{
	using System.IO;

	internal static class StreamExtensions
	{
		private const int CarriageReturn = '\r';
		private const int LineFeed = '\n';

		/// <summary>
		/// Indicates whether a line begins at the specified byte offset.
		/// </summary>
		/// <remarks>
		/// Recognizes the same line endings as <see cref="StreamReader.ReadLine"/>: <c>\n</c>, <c>\r</c> and <c>\r\n</c>.
		/// Line endings that span several bytes, such as in UTF-16 little-endian files, are not recognized.
		/// The position of the stream is restored.
		/// </remarks>
		public static bool IsStartOfLine(this Stream stream, long byteOffset)
		{
			if (byteOffset < 0 || byteOffset >= stream.Length)
			{
				return false;
			}

			if (byteOffset == 0)
			{
				return true;
			}

			var originalPosition = stream.Position;

			try
			{
				stream.Position = byteOffset - 1;

				var previous = stream.ReadByte();
				var current = stream.ReadByte();

				return previous == LineFeed || (previous == CarriageReturn && current != LineFeed);
			}
			finally
			{
				stream.Position = originalPosition;
			}
		}
	}
}
