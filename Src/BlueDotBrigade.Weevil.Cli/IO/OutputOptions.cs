namespace BlueDotBrigade.Weevil.Cli.IO
{
	using BlueDotBrigade.Weevil.IO;

	/// <summary>
	/// The output format that <c>--output-as</c> chose, which <see cref="Program"/> gives to the commands (#837).
	/// </summary>
	internal sealed record OutputOptions(OutputFormat Format);
}
