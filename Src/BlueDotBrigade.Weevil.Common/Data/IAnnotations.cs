namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Collections.Generic;

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

		/// <summary>
		/// The line has a comment that is not only white space.
		/// </summary>
		bool HasComment(int lineNumber);

		bool IsPinned(int lineNumber);

		/// <summary>
		/// Adding a comment pins the record, and removing it unpins the record.
		/// </summary>
		/// <param name="source">The object making the change, passed on to <see cref="Changed"/> so it can ignore its own writes.</param>
		void SetComment(int lineNumber, string comment, object source = null);

		/// <summary>
		/// Adds <paramref name="comment"/> to the end of the line's comment, after a comma, or sets it when the line has none.
		/// </summary>
		/// <remarks>
		/// One step, so a comment written at the same time is not lost. As <see cref="SetComment(int, string, object)"/> does, adding a comment pins the record.
		/// </remarks>
		/// <param name="source">The object making the change, passed on to <see cref="Changed"/> so it can ignore its own writes.</param>
		void AppendComment(int lineNumber, string comment, object source = null);

		/// <param name="source">The object making the change, passed on to <see cref="Changed"/> so it can ignore its own writes.</param>
		void SetPinned(int lineNumber, bool isPinned, object source = null);

		/// <summary>
		/// Sets the same comment on many lines in one change: <see cref="Changed"/> is raised once, for <see cref="AnnotationChangedEventArgs.ManyLines"/>.
		/// </summary>
		/// <remarks>
		/// As <see cref="SetComment(int, string, object)"/> does, adding a comment pins a line and removing it unpins the line.
		/// </remarks>
		void SetComment(IEnumerable<int> lineNumbers, string comment, object source = null);

		/// <summary>
		/// Pins or unpins many lines in one change: <see cref="Changed"/> is raised once, for <see cref="AnnotationChangedEventArgs.ManyLines"/>.
		/// </summary>
		/// <remarks>
		/// The comments stay.
		/// </remarks>
		void SetPinned(IEnumerable<int> lineNumbers, bool isPinned, object source = null);

		/// <summary>
		/// Removes the comments of many lines in one change: <see cref="Changed"/> is raised once, for <see cref="AnnotationChangedEventArgs.ManyLines"/>.
		/// </summary>
		/// <remarks>
		/// As <see cref="SetComment(int, string, object)"/> does, removing a comment unpins its line.
		/// </remarks>
		void RemoveComments(IEnumerable<int> lineNumbers, object source = null);
	}
}
