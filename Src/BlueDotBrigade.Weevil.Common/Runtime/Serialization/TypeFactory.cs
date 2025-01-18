namespace BlueDotBrigade.Weevil.Runtime.Serialization
{
	using System.IO;
	using System.Runtime.Serialization;
	using System.Xml;
	using BlueDotBrigade.Weevil.IO;

	public class TypeFactory
	{
		/// <remarks>
		/// <b>KNOWN ISSUE:</b> this method ignores the <c>[DataMember(Order=123)]</c> attribute and will
		/// always save the XML elements in alphabetical order.
		/// </remarks>
		public static void SaveAsXml(object value, string path)
		{
			var settings = new XmlWriterSettings()
			{
				Indent = true,
				IndentChars = "\t",
				NamespaceHandling = NamespaceHandling.OmitDuplicates,
			};
			SaveAsXml(value, path, settings);
		}

		/// <remarks>
		/// <b>KNOWN ISSUE:</b> this method ignores the <c>[DataMember(Order=123)]</c> attribute and will
		/// always save the XML elements in alphabetical order.
		/// </remarks>
		public static void SaveAsXml(object value, string path, XmlWriterSettings settings)
		{
			using (FileStream fileStream = FileHelper.Open(path, FileMode.Create, FileAccess.Write))
			{
				using (var xmlWriter = XmlWriter.Create(fileStream, settings))
				{
					var serializer = new DataContractSerializer(value.GetType());
					serializer.WriteObject(xmlWriter, value);
				}
				
				fileStream.Close();
			}
		}

		/// <remarks>
		/// <b>KNOWN ISSUE:</b> The XML elements must be in alphabetical order. If not, then empty values will be deserialized.
		/// </remarks>
		public static T LoadFromXml<T>(string path)
		{
			var result = default(T);

			using (FileStream fileStream = FileHelper.Open(path))
			{
				result = LoadFromXml<T>(fileStream);
				fileStream.Close();
			}

			return result;
		}

		/// <remarks>
		/// <b>KNOWN ISSUE:</b> The XML elements must be in alphabetical order. If not, then empty values will be deserialized.
		/// </remarks>
		public static T LoadFromXml<T>(Stream serializedData)
		{
			var result = default(T);

			using (var reader = XmlDictionaryReader.CreateTextReader(serializedData, new XmlDictionaryReaderQuotas()))
			{
				var deserializer = new DataContractSerializer(typeof(T));

				result = (T)deserializer.ReadObject(reader, true);

				reader.Close();
			}

			return result;
		}

	}
}
