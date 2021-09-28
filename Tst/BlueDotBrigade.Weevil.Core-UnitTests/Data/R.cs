namespace BlueDotBrigade.Weevil.Data
{
	using System;

	/// <summary>
	/// A record factory used to create fake data for testing.
	/// </summary>
	internal static class R
	{
		/// <summary>
		/// Creates a fake record with the provided <paramref name="lineNumber"/>.
		/// </summary>
		public static IRecord WithLineNumber(int lineNumber)
		{
			return new Record(
				lineNumber,
				DateTime.Now.AddSeconds(lineNumber),
				SeverityType.Information,
				$"Fake record used for testing. Has line number: {lineNumber}",
				new Metadata());
		}
	}
}
