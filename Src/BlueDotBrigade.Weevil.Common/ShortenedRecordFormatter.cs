namespace BlueDotBrigade.Weevil
{
	using System.Diagnostics;
	using BlueDotBrigade.Weevil.Data;

	public class ShortenedRecordFormatter : IRecordFormatter
	{
		internal static readonly string EndOfLine = "\u22EF";

		private readonly int _maximumLength;
		private readonly int _truncatedLength;

		public ShortenedRecordFormatter(int maximumLength, int truncatedLength)
		{
			Debug.Assert(maximumLength > truncatedLength);

			_maximumLength = maximumLength;
			_truncatedLength = truncatedLength;
		}

		public string Format(IRecord record)
		{
			return Format(record.Content);
		}

		public string Format(string content)
		{
			var result = content;

			if (content.Length >= _maximumLength)
			{
				var maxLength = content.Length >= _maximumLength
					? _truncatedLength
					: content.Length;
				result = content.Substring(0, maxLength) + EndOfLine;
			}

			return result;
		}
	}
}