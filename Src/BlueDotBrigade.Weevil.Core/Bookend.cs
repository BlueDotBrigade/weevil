namespace BlueDotBrigade.Weevil
{
	internal class Bookend
	{
		public int StartLineNumber { get; }
		public int EndLineNumber { get; }

		public Bookend(int startLineNumber, int endLineNumber)	
		{	
			this.StartLineNumber = startLineNumber;
			this.EndLineNumber = endLineNumber;
		}	

		public bool OverlapsWith(Bookend other)
		{
			return this.StartLineNumber <= other.EndLineNumber && this.EndLineNumber >= other.StartLineNumber;
		}

		public bool Contains(int lineNumber)
		{
			return lineNumber >= this.StartLineNumber && lineNumber <= this.EndLineNumber;
		}
	}
}