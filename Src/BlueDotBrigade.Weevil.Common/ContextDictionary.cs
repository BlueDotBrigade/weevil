namespace BlueDotBrigade.Weevil
{
	using System.Collections.Generic;
	using System.Runtime.Serialization;

	[CollectionDataContract
	(Name = "Context",
		ItemName = "Property",
		KeyName = "Name",
		ValueName = "Value",
		Namespace = "")]
	public class ContextDictionary : Dictionary<string, string>
	{
		public static ContextDictionary Empty => new ContextDictionary();

		public override string ToString()
		{
			var resultString = string.Empty;
			foreach (KeyValuePair<string, string> property in this)
			{
				resultString += string.Format("{0}={1}; ",
					property.Key,
					property.Value);
			}

			return resultString.Trim();
		}
	}
}
