namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Threading;

	/// <summary>
	/// The state of the records one view of a log shows (decision 7 of the #990 plan): the log's comments and pins,
	/// shared by every view, and the view's own flags and elapsed times.
	/// </summary>
	/// <remarks>
	/// It only forwards to the stores, which keep their own locks. Bulk changes go to the stores directly,
	/// so many lines change under one lock with one event.
	/// </remarks>
	public sealed class RecordState : IRecordState
	{
		private ElapsedTimes _elapsedTimes = ElapsedTimes.None;

		public RecordState()
			: this(new AnnotationStore(), new FlagSet())
		{
			// nothing to do
		}

		public RecordState(AnnotationStore annotations, FlagSet flags)
		{
			this.Annotations = annotations ?? throw new ArgumentNullException(nameof(annotations));
			this.Flags = flags ?? throw new ArgumentNullException(nameof(flags));
		}

		/// <summary>
		/// The log's comments and pins, for bulk changes and change events.
		/// </summary>
		public AnnotationStore Annotations { get; }

		/// <summary>
		/// The view's flags, for bulk changes.
		/// </summary>
		public FlagSet Flags { get; }

		/// <summary>
		/// The elapsed times of the records the view shows (#1016). The view replaces them after each filter.
		/// </summary>
		public ElapsedTimes ElapsedTimes
		{
			get => Volatile.Read(ref _elapsedTimes);
			set => Volatile.Write(ref _elapsedTimes, value ?? throw new ArgumentNullException(nameof(value)));
		}

		public string GetComment(IRecord record) => this.Annotations.GetComment(record.LineNumber);

		public bool HasComment(IRecord record) => this.Annotations.HasComment(record.LineNumber);

		public bool IsPinned(IRecord record) => this.Annotations.IsPinned(record.LineNumber);

		public bool IsFlagged(IRecord record) => this.Flags.IsFlagged(record.LineNumber);

		public TimeSpan GetElapsedTime(IRecord record) => this.ElapsedTimes.Get(record.LineNumber);

		public void SetComment(IRecord record, string comment) => this.Annotations.SetComment(record.LineNumber, comment);

		public void AppendComment(IRecord record, string comment) => this.Annotations.AppendComment(record.LineNumber, comment);

		public void SetPinned(IRecord record, bool isPinned) => this.Annotations.SetPinned(record.LineNumber, isPinned);

		public void SetFlagged(IRecord record, bool isFlagged) => this.Flags.SetFlagged(record.LineNumber, isFlagged);
	}
}
