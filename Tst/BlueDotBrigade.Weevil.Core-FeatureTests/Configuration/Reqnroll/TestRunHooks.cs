namespace BlueDotBrigade.Weevil.Configuration.Reqnroll
{
	using System;
	using System.IO;
	using BlueDotBrigade.DatenLokator.TestTools.Configuration;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.TestTools.Configuration.Reqnroll;

	/// <summary>
	/// Represents Reqnroll events that execute before & after every test run.
	/// </summary>
	/// <seealso href="https://docs.reqnroll.net/latest/automation/hooks.html">Reqnroll: Hooks</seealso>
	[Binding]
	internal class TestRunHooks
	{
		[BeforeTestRun(Order = Constants.AlwaysFirst)]
		public static void Setup(ITestRunnerManager testRunnerManager)
		{
			Log.Default.Write(LogSeverityType.Debug, "Reqnroll test environment is being setup...");

			// Workaround for DatenLokator path separator issue on Linux
			// Ensure .Daten directory exists in current working directory
			var currentDir = Directory.GetCurrentDirectory();
			var datenPath = Path.Combine(currentDir, ".Daten");
			
			// If .Daten doesn't exist in current directory, try to copy it from project source
			if (!Directory.Exists(datenPath))
			{
				var projectDatenPath = Path.GetFullPath(Path.Combine(currentDir, "..", "..", "..", ".Daten"));
				if (Directory.Exists(projectDatenPath))
				{
					CopyDirectory(projectDatenPath, datenPath);
					Log.Default.Write(LogSeverityType.Information, $"Copied .Daten directory from {projectDatenPath} to {datenPath}");
				}
			}

			Lokator
				.Get()
				.UsingDefaultFileName("Droid.log")
				.Setup();

			Log.Default.Write(LogSeverityType.Information, "Reqnroll test environment has been setup.");
		}

		private static void CopyDirectory(string sourceDir, string destDir)
		{
			Directory.CreateDirectory(destDir);
			foreach (var file in Directory.GetFiles(sourceDir))
			{
				var destFile = Path.Combine(destDir, Path.GetFileName(file));
				File.Copy(file, destFile, true);
			}
			foreach (var subDir in Directory.GetDirectories(sourceDir))
			{
				var destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
				CopyDirectory(subDir, destSubDir);
			}
		}

		[AfterTestRun(Order = Constants.AlwaysLast)]
		public static void Teardown(ITestRunnerManager testRunnerManager)
		{
			Log.Default.Write(LogSeverityType.Debug, "Reqnroll test environment is being torn down...");

			Lokator
				.Get()
				.TearDown();

			Log.Default.Write(LogSeverityType.Information, "Reqnroll test environment has been torn down.");
		}
	}
}