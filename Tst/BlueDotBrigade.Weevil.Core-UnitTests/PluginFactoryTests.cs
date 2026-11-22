namespace BlueDotBrigade.Weevil
{
	using System;
	using System.ComponentModel.Composition;
	using System.IO;
	using System.Reflection;
	using System.Reflection.Emit;

	[TestClass]
	public class PluginFactoryTests
	{
		private string _folder = string.Empty;

		public interface IPartUnderTest
		{
		}

		[TestInitialize]
		public void CreateFolder()
		{
			_folder = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), nameof(PluginFactoryTests), Guid.NewGuid().ToString("N"))).FullName;
		}

		[TestCleanup]
		public void DeleteFolder()
		{
			try
			{
				Directory.Delete(_folder, recursive: true);
			}
			catch (Exception e) when (e is IOException or UnauthorizedAccessException)
			{
				// Windows keeps an assembly the test loaded until the process ends, so the folder stays in the temporary folder.
			}
		}

		[TestMethod]
		public void GivenAPluginThatCantBeLoadedOrCreated_WhenPluginsAreLoaded_ThenTheOthersAreStillLoaded()
		{
			// One assembly that failed to load stopped every plugin, and so every open, in the GUI, the CLI and PowerShell (#1216):
			// for example one built against another version of Weevil, or one that needs WPF. ThrowingPart's constructor throws, as one missing its license might.
			File.Copy(typeof(PluginFactoryTests).Assembly.Location, Path.Combine(_folder, "Healthy.dll"));
			SaveAssemblyThatCantBeLoaded(Path.Combine(_folder, "Broken.dll"));
			File.WriteAllText(Path.Combine(_folder, "Native.dll"), "Not a .NET assembly, such as a native library a plugin uses.");

			PluginFactory.LoadExports<IPartUnderTest>(_folder)
				.Should().ContainSingle()
				.Which.Should().BeOfType<PartUnderTest>();
		}

		/// <summary>
		/// Saves an assembly whose type implements an interface from an assembly that isn't there, so reading its types fails, as for a plugin whose dependency is missing.
		/// </summary>
		private static void SaveAssemblyThatCantBeLoaded(string path)
		{
			var missing = new PersistedAssemblyBuilder(new AssemblyName("Weevil.MissingContract"), typeof(object).Assembly);
			TypeBuilder contract = missing.DefineDynamicModule("Contract").DefineType("IMissing", TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
			contract.CreateType();

			var broken = new PersistedAssemblyBuilder(new AssemblyName("Weevil.BrokenPlugin"), typeof(object).Assembly);
			TypeBuilder plugin = broken.DefineDynamicModule("Plugin").DefineType("BrokenPlugin", TypeAttributes.Public | TypeAttributes.Class, typeof(object), new Type[] { contract });
			plugin.DefineDefaultConstructor(MethodAttributes.Public);
			plugin.CreateType();

			broken.Save(path);
		}

		[Export(typeof(IPartUnderTest))]
		public sealed class PartUnderTest : IPartUnderTest
		{
		}

		[Export(typeof(IPartUnderTest))]
		public sealed class ThrowingPart : IPartUnderTest
		{
			public ThrowingPart()
			{
				throw new InvalidOperationException("The plugin's license file is missing.");
			}
		}
	}
}
