namespace BlueDotBrigade.Weevil.Filter.Expressions
{
	using System;
	using System.Runtime.Serialization;

	[Serializable]
	public class MatchCountException : Exception
	{
		private static readonly string DefaultMessage = "The provided regular expression does not match the correct number of values. Expression=`{0}`";

		public MatchCountException(string expression)
			: base(string.Format(DefaultMessage, expression))
		{
			this.Expression = expression;
		}

		public MatchCountException(string expression, Exception innerException)
			: base(string.Format(DefaultMessage, expression), innerException)
		{
			this.Expression = expression;
		}

		public MatchCountException(string expression, string message)
			: base(message)
		{
			this.Expression = expression;
		}

		public MatchCountException(string expression, string message, Exception innerException)
			: base(message, innerException)
		{
			this.Expression = expression;
		}

		protected MatchCountException(SerializationInfo info, StreamingContext context)
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
