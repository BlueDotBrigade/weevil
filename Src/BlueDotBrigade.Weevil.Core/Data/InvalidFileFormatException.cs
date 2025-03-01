namespace BlueDotBrigade.Weevil.Data
{
	using System;
	using System.Runtime.Serialization;
	using System.Security.Permissions;

	[Serializable]
	public class InvalidFileFormatException : Exception
	{
		private static readonly string DefaultMessage = "The specified file cannot be parsed. Path=`{0}`";

		public InvalidFileFormatException(string path)
			: base(string.Format(DefaultMessage, path))
		{
			this.Path = path;
		}

		public InvalidFileFormatException(string path, Exception innerException)
			: base(string.Format(DefaultMessage, path), innerException)
		{
			this.Path = path;
		}

		public InvalidFileFormatException(string path, string message)
			: base(message)
		{
			this.Path = path;
		}

		public InvalidFileFormatException(string path, string message, Exception innerException)
			: base(message, innerException)
		{
			this.Path = path;
		}

		protected InvalidFileFormatException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			this.Path = info.GetString(nameof(this.Path));
		}

		public string Path { get; }

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
			{
				throw new ArgumentNullException(nameof(info));
			}

			info.AddValue(nameof(this.Path), this.Path);
			base.GetObjectData(info, context);
		}

		public override string ToString()
		{
			return $"{base.ToString()}, {nameof(this.Path)}: {this.Path}";
		}
	}
}
