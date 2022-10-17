namespace BlueDotBrigade.Weevil.Common.Runtime.Serialization
{
	using System.Collections.Generic;
	using System.IO;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using BlueDotBrigade.DatenLokator.TestsTools;
	using Weevil.Runtime.Serialization;

	[TestClass]
	public class TypeFactoryTest
	{
		public class Dog
		{
			public string Name { get; set; }
			public int Age { get; set; }
		}

		public class Mammal
		{
			public Mammal()
			{
				this.Dogs = new List<Dog>();
			}

			public List<Dog> Dogs { get; set; }
		}

		[TestMethod]
		public void SaveAsXml_SimpleObjectWithList_FileCreated()
		{
			var metadata = new TypeFactoryTest.Mammal
			{
				Dogs = new List<TypeFactoryTest.Dog>
				{
					{new TypeFactoryTest.Dog { Name = "Pavlov", Age = 1}},
					{new TypeFactoryTest.Dog { Name = "Lassie", Age = 2}},
					{new TypeFactoryTest.Dog { Name = "Littlest Hobo", Age = 3}},
				}
			};

			var filePath = Path.GetTempFileName();
			try
			{
				TypeFactory.SaveAsXml(metadata, filePath);

				Assert.IsTrue(File.Exists(filePath));
			}
			finally
			{
				File.Delete(filePath);
			}
		}

		[TestMethod]
		public void LoadFromXml_SerializedList_ObjectGraphReturned()
		{
			Stream inputData = new Daten().AsStream();
			Mammal metadata = TypeFactory.LoadFromXml<TypeFactoryTest.Mammal>(inputData);

			Assert.IsNotNull(metadata);
		}
	}
}
