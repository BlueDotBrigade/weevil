namespace BlueDotBrigade.Weevil.Test
{
	using System.IO;
	using System.Text;

	public static class EncodingHelper
	{
		/// <summary>
		/// Automatically detects the text encoding for the given file.
		/// </summary>
		/// <param name="filePath">The path of the file to open.</param>
		/// <returns></returns>
		/// <see href="https://docs.microsoft.com/en-us/dotnet/api/system.io.streamreader.currentencoding?view=net-6.0">MSDN: StreamReader.CurrentEncoding</see>
		/// <remarks>
		/// As per the MSDN documentation:
		/// <list type="bullet">
		///    <item>encoding auto detection is not done until the first call to a <see cref="StreamReader.Read()"/> method</item>
		/// </list>
		/// </remarks>
		public static Encoding GetEncoding(string filePath)
		{
			using (var reader = new StreamReader(filePath, Encoding.Default, detectEncodingFromByteOrderMarks: true))
			{
				if (reader.Peek() >= 0)
				{
					reader.Read();
				}

				return reader.CurrentEncoding;
			}
		}

		/// <summary>
		/// Automatically detects the text encoding for the given file.
		/// </summary>
		/// <param name="sourceStream">The stream to be read, which will be closed after the operation completes.</param>
		/// <returns>The file&apos;s encoding scheme.</returns>
		/// <see href="https://docs.microsoft.com/en-us/dotnet/api/system.io.streamreader.currentencoding?view=net-6.0">MSDN: StreamReader.CurrentEncoding</see>
		/// <remarks>
		/// As per the MSDN documentation:
		/// <list type="bullet">
		///    <item>encoding auto detection is not done until the first call to a <see cref="StreamReader.Read()"/> method</item>
		/// </list>
		/// </remarks>
		public static Encoding GetEncoding(FileStream sourceStream)
		{
			using (var reader = new StreamReader(sourceStream, Encoding.Default, detectEncodingFromByteOrderMarks: true))
			{
				if (reader.Peek() >= 0)
				{
					reader.Read();
				}

				return reader.CurrentEncoding;
			}
		}
	}
}
