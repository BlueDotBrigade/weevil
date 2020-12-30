namespace BlueDotBrigade.Weevil
{
	using System.Diagnostics;

	[DebuggerDisplay("Minimum={Minimum}, Maximum={Maximum}")]
	public class Range
	{
		public static readonly Range Complete = new Range();

		public Range()
		{
			this.Minimum = int.MinValue;
			this.Maximum = int.MaxValue;
		}

		public Range(int minimum, int maximum)
		{
			this.Minimum = minimum;
			this.Maximum = maximum;
		}

		public int Minimum { get; }
		public int Maximum { get; }

		public bool IsCompleteRange =>
			this.Minimum == int.MinValue &&
			this.Maximum == int.MaxValue;

		/// <inheritdoc />
		public override string ToString()
		{
			return $"{this.Minimum}..{this.Maximum}";
		}
	}
}
