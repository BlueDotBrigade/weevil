namespace BlueDotBrigade.Weevil.Filter.Expressions
{
	using System;
	using System.Runtime.Serialization;
	using System.Security.Permissions;

	[Serializable]
	public class InvalidExpressionException : Exception
	{
		private static readonly string DefaultMessage = "The regular expression is not valid. Expression=`{0}`";

		public InvalidExpressionException(string expression)
			: base(string.Format(DefaultMessage, expression))
		{
			this.Expression = expression;
		}

		public InvalidExpressionException(string expression, Exception innerException)
			: base(string.Format(DefaultMessage, expression), innerException)
		{
			this.Expression = expression;
		}

		public InvalidExpressionException(string expression, string message)
			: base(message)
		{
			this.Expression = expression;
		}

		public InvalidExpressionException(string expression, string message, Exception innerException)
			: base(message, innerException)
		{
			this.Expression = expression;
		}

		protected InvalidExpressionException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			this.Expression = info.GetString(nameof(this.Expression));
		}

		public string Expression { get; }

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException(nameof(info));
			}

			info.AddValue(nameof(this.Expression), this.Expression);
			base.GetObjectData(info, context);
		}

		public override string ToString()
		{
			return $"{base.ToString()}, {nameof(this.Expression)}: {this.Expression}";
		}
	}
}
