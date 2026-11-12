namespace BlueDotBrigade.Weevil.Cli.Security
{
	using Cocona;
	using OutputFormat = BlueDotBrigade.Weevil.IO.OutputFormat;
	using OutputOptions = BlueDotBrigade.Weevil.Cli.IO.OutputOptions;

	[TestClass]
	public class SecureCommandsTests
	{
		[TestMethod]
		public void GivenOutputAsJson_WhenASecretIsEncrypted_ThenTheCommandExitsWithAnError()
		{
			// Encrypt writes text, which a program reading JSON would fail on, so it refuses (#837).
			var encrypt = () => new SecureCommands(new OutputOptions(OutputFormat.Json)).Encrypt("secret");

			encrypt.Should().Throw<CommandExitedException>()
				.Which.ExitCode.Should().Be(1);
		}
	}
}
