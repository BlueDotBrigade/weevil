namespace BlueDotBrigade.Weevil.Filter
{
	using System;
	using System.Runtime.Serialization;
	using System.Security.Permissions;

	[Serializable]
	public class FilterAbortedException : Exception
	{
		private const string DefaultMessage = "The filtering operation has been aborted.";

		public FilterAbortedException() : this(DefaultMessage)
		{
			// nothing to do
		}

		public FilterAbortedException(string message) : base(message)
		{
			// nothing to do
		}

		public FilterAbortedException(string message, Exception innerException)
			: base(message, innerException)
		{
			// nothing to do
		}

		protected FilterAbortedException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			// no parameters to deserialize
		}


		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException(nameof(info));
			}

			base.GetObjectData(info, context);
		}
	}
}
