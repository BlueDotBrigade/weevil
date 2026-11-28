namespace BlueDotBrigade.Weevil
{
	using System;

	public class UninitializedValueException : Exception
    {
		public UninitializedValueException(string message) : base(message)
		{
			// nothing to do
		}
    }
}