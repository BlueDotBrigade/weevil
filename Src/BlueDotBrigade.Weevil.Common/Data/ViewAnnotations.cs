namespace BlueDotBrigade.Weevil.Data
{
	using System;

	/// <summary>
	/// What one view of a log adds to its records (#1013): the log's comments and pins, shared by every view, and the view's own flags.
	/// </summary>
	public sealed class ViewAnnotations
	{
		public ViewAnnotations()
			: this(new AnnotationStore(), new FlagSet())
		{
			// nothing to do
		}

		public ViewAnnotations(AnnotationStore annotations, FlagSet flags)
		{
			this.Annotations = annotations ?? throw new ArgumentNullException(nameof(annotations));
			this.Flags = flags ?? throw new ArgumentNullException(nameof(flags));
		}

		public AnnotationStore Annotations { get; }

		public FlagSet Flags { get; }
	}
}
