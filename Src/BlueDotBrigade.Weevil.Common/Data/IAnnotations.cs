namespace BlueDotBrigade.Weevil.Data
{
	using System;

	/// <summary>
	/// The comments and pins of a log, kept by line number and shared by every view of it.
	/// </summary>
	public interface IAnnotations
	{
		/// <summary>
		/// Raised after a comment or pin changes, on the thread that changed it.
		/// </summary>
		/// <remarks>
		/// Not raised when the value does not change, nor when the sidecar is loaded.
		/// </remarks>
		event EventHandler<AnnotationChangedEventArgs> Changed;

		string GetComment(int lineNumber);

		bool IsPinned(int lineNumber);

		/// <summary>
		/// Adding a comment pins the record, and removing it unpins the record.
		/// </summary>
		/// <param name="source">The object making the change, passed on to <see cref="Changed"/> so it can ignore its own writes.</param>
		void SetComment(int lineNumber, string comment, object source = null);

		/// <param name="source">The object making the change, passed on to <see cref="Changed"/> so it can ignore its own writes.</param>
		void SetPinned(int lineNumber, bool isPinned, object source = null);
	}
}
