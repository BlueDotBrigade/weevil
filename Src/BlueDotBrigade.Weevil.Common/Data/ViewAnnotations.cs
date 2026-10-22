namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Threading;

	/// <summary>
	/// What one view of a log adds to its records (#1013): the log's comments and pins, shared by every view,
	/// and the view's own flags and elapsed times.
	/// </summary>
	public sealed class ViewAnnotations
	{
		private ElapsedTimes _elapsedTimes = ElapsedTimes.None;

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

		/// <summary>
		/// The elapsed times of the records the view shows (#1016). The view replaces them after each filter.
		/// </summary>
		public ElapsedTimes ElapsedTimes
		{
			get => Volatile.Read(ref _elapsedTimes);
			set => Volatile.Write(ref _elapsedTimes, value ?? throw new ArgumentNullException(nameof(value)));
		}
	}
}
