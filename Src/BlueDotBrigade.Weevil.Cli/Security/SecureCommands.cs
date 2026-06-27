namespace BlueDotBrigade.Weevil.Cli.Security
{
	using BlueDotBrigade.Weevil.Diagnostics;
	using IO;
	using Cocona;
	using Cocona.Help;

	[TransformHelpFactory(typeof(SecureCommandHelp))]
	internal class SecureCommands
	{
		[Command(
			name: "encrypt",
			Aliases = new[] { "e" },
			Description = "Encrypts a telemetry secret so it can be stored safely in an environment variable.")]
		public void Encrypt(
			[Option(Description = "The plaintext secret to encrypt.")]
			string secret)
		{
			var encrypted = SecretProtector.Encrypt(secret);

			Write.Text("Encrypted value (copy this to your environment variable):");
			Write.Text(encrypted);
		}
	}
}
