namespace BlueDotBrigade.Weevil.Data
{
	using System;

	public sealed class AnnotationChangedEventArgs : EventArgs
	{
		/// <summary>
		/// The <see cref="LineNumber"/> of one change to many lines, such as Unpin All.
		/// </summary>
		public const int ManyLines = -1;

		public AnnotationChangedEventArgs(int lineNumber, object source)
		{
			this.LineNumber = lineNumber;
			this.Source = source;
		}

		public int LineNumber { get; }

		/// <summary>
		/// The object that made the change, or <see langword="null"/> when the caller did not say.
		/// </summary>
		public object Source { get; }
	}
}
