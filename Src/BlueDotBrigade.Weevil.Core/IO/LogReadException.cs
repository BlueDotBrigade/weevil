namespace BlueDotBrigade.Weevil.IO
{
	using System;
	using System.IO;

	/// <summary>
	/// The log could not be read while the index saved beside it was checked against it, as opposed to the saved index.
	/// </summary>
	/// <remarks>
	/// The saved index may still be the log's, so <see cref="SavedIndex"/> keeps it. <see cref="Exception.InnerException"/> holds what the read threw.
	/// </remarks>
	internal sealed class LogReadException : Exception
	{
		public LogReadException(IOException readFailure)
			: base("The log could not be read.", readFailure)
		{
		}
	}
}
