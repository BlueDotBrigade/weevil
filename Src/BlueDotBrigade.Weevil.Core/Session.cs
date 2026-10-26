namespace BlueDotBrigade.Weevil
{
	using System;
	using BlueDotBrigade.Weevil.Data;

	/// <summary>
	/// The mutable state of an open log, shared by every view of it, so records hold none (decision 6 of the #990 plan).
	/// </summary>
	/// <remarks>
	/// Each kind of state is a child store, which serializes its own writes. For now the session holds the comments and pins;
	/// bookmarks, regions, remarks, the table of contents and the filter history join it later.
	/// It lasts as long as the log is open: Clear and Reload keep it.
	/// </remarks>
	internal sealed class Session
	{
		public Session(AnnotationStore annotations)
		{
			this.Annotations = annotations ?? throw new ArgumentNullException(nameof(annotations));
		}

		/// <summary>
		/// The comments and pins, by line number.
		/// </summary>
		public AnnotationStore Annotations { get; }
	}
}
