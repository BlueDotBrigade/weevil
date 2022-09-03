namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Runtime.Serialization;
	using System.Security.Permissions;

	[Serializable]
	public class RecordNotFoundException : Exception
	{
		private static readonly string DefaultMessage = "Unable to find record.";

		public RecordNotFoundException()
			: base(string.Format(DefaultMessage))
		{
			// nothing to do
		}

		public RecordNotFoundException(Exception innerException)
			: base(DefaultMessage, innerException)
		{
			// nothing to do
		}

		public RecordNotFoundException(string message)
			: base(message)
		{
			// nothing to do
		}

		public RecordNotFoundException(string message, Exception innerException)
			: base(message, innerException)
		{
			// nothing to do
		}

		protected RecordNotFoundException(SerializationInfo info, StreamingContext context)
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
