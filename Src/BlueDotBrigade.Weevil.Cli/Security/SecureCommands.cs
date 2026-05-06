namespace BlueDotBrigade.Weevil.Cli.Security
{
	using BlueDotBrigade.Weevil.Diagnostics;
	using Cocona;
	using Cocona.Help;

	[TransformHelpFactory(typeof(SecureCommandHelp))]
	internal class SecureCommands
	{
		[Command(
			name: "protect-secret",
			Aliases = new[] { "ps" },
			Description = "Encrypts a telemetry secret so it can be stored safely in an environment variable.")]
		public void ProtectSecret(
			[Option(Description = "The plaintext secret to encrypt.")]
			string secret)
		{
			var encrypted = SecretProtector.Protect(secret);

			Write.Text("Encrypted value (copy this to your environment variable):");
			Write.Text(encrypted);
		}
	}
}
