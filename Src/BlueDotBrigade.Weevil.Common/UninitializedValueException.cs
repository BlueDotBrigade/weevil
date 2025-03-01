using System;

namespace BlueDotBrigade.Weevil
{
    public class UninitializedValueException : Exception
    {
		public UninitializedValueException(string message) : base(message)
		{
			// nothing to do
		}
    }
}