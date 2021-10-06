namespace BlueDotBrigade.Weevil.Navigation
{
	using System;
	using System.Runtime.Serialization;
	using System.Security.Permissions;

	[Serializable]
	public class RecordNotFoundException : Exception
	{
		private static readonly string DefaultMessage = "Record could not be found. LineNumber=`{0}`";

		public RecordNotFoundException(int lineNumber)
			: base(string.Format(DefaultMessage, lineNumber))
		{
			this.LineNumber = lineNumber;
		}

		public RecordNotFoundException(int lineNumber, Exception innerException)
			: base(string.Format(DefaultMessage, lineNumber), innerException)
		{
			this.LineNumber = lineNumber;
		}

		public RecordNotFoundException(int lineNumber, string message)
			: base(message)
		{
			this.LineNumber = lineNumber;
		}

		public RecordNotFoundException(int lineNumber, string message, Exception innerException)
			: base(message, innerException)
		{
			this.LineNumber = lineNumber;
		}

		protected RecordNotFoundException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			var serializedValue = info.GetString(nameof(this.LineNumber));
			this.LineNumber = int.Parse(serializedValue);
		}

		public int LineNumber { get; }

		[SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException(nameof(info));
			}

			info.AddValue(nameof(this.LineNumber), this.LineNumber);
			base.GetObjectData(info, context);
		}

		public override string ToString()
		{
			return $"{base.ToString()}, {nameof(this.LineNumber)}: {this.LineNumber}";
		}
	}
}
