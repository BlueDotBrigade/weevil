namespace BlueDotBrigade.Weevil.Cli.Security
{
	using BlueDotBrigade.Weevil.Diagnostics;
	using IO;
	using Cocona;
	using Cocona.Help;

	[TransformHelpFactory(typeof(SecureCommandHelp))]
	internal class SecureCommands
	{
		private readonly OutputOptions _output;

		public SecureCommands(OutputOptions output)
		{
			_output = output ?? throw new System.ArgumentNullException(nameof(output));
		}

		[Command(
			name: "encrypt",
			Aliases = new[] { "e" },
			Description = "Encrypts a telemetry secret so it can be stored safely in an environment variable.")]
		public void Encrypt(
			[Option(Description = "The plaintext secret to encrypt.")]
			string secret)
		{
			if (_output.Format == BlueDotBrigade.Weevil.IO.OutputFormat.Json)
			{
				// A program reading JSON would fail on text, so the command says so instead (#837).
				throw new CommandExitedException("encrypt writes text only. Use --output-as markdown or plaintext.", 1);
			}

			var encrypted = SecretProtector.Encrypt(secret);

			Write.Text("Encrypted value (copy this to your environment variable):");
			Write.Text(encrypted);
		}
	}
}
