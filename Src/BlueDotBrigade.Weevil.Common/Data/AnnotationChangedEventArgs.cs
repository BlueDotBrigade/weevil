namespace BlueDotBrigade.Weevil.Data
{
	using System;

	public sealed class AnnotationChangedEventArgs : EventArgs
	{
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
