namespace BlueDotBrigade.Weevil.Gui
{
	using System;
	using System.Runtime.Serialization;
	using System.Security.Permissions;

	[Serializable]
	public class DuplicateSubscriptionException : Exception
	{
		private static readonly string DefaultMessage = "Recipient has already subscribed to this bulletin.";

		public DuplicateSubscriptionException()
			: base(string.Format(DefaultMessage))
		{
			// nothing to do
		}

		public DuplicateSubscriptionException(Exception innerException)
			: base(DefaultMessage, innerException)
		{
			// nothing to do
		}

		public DuplicateSubscriptionException(string message)
			: base(message)
		{
			// nothing to do
		}

		public DuplicateSubscriptionException(string message, Exception innerException)
			: base(message, innerException)
		{
			// nothing to do
		}

		protected DuplicateSubscriptionException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			// nothing to do
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
