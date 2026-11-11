namespace BlueDotBrigade.Weevil
{
	using BenchmarkDotNet.Configs;
	using BenchmarkDotNet.Running;

	/// <summary>
	/// The entry point of the benchmarks: one of this project's two entry points (#1177).
	/// </summary>
	/// <remarks>
	/// This project has two entry points, for two kinds of performance check:
	/// <list type="bullet">
	///   <item>
	///     <term><c>dotnet test</c></term>
	///     <description>
	///     Runs the tests: the MSTest <c>[TestClass]</c> classes, which check budgets that must hold, in CI and in Visual Studio's Test Explorer.
	///     The test runner loads the assembly itself, and never calls <see cref="Main"/>.
	///     </description>
	///   </item>
	///   <item>
	///     <term><c>dotnet run -c Release -p:Platform=x64 --project Tst/BlueDotBrigade.Weevil.Core-PerformanceTests -- --filter "*"</c></term>
	///     <description>
	///     Calls <see cref="Main"/>, which runs the BenchmarkDotNet benchmarks in the <c>Benchmarks</c> folder, to compare two versions.
	///     The arguments after <c>--</c> go to BenchmarkDotNet: see Doc/Guides/Testing/Testing.md.
	///     </description>
	///   </item>
	/// </list>
	/// BenchmarkDotNet's documentation says it works only with console applications ("How to run your benchmarks", benchmarkdotnet.org).
	/// A test method can start it, but that is not a supported way to run it, and <c>dotnet test</c>, and so CI, would then run benchmarks that take minutes.
	/// Starting them from this method follows the documentation, and keeps them out of every test run.
	///
	/// With its default settings, BenchmarkDotNet refuses to measure code that is not optimized, because the times would not mean anything.
	/// So a Debug build only says how to run the benchmarks in Release.
	/// </remarks>
	public static class Program
	{
		/// <param name="args">BenchmarkDotNet's options, for example <c>--filter *FilterBenchmarks*</c> or <c>--job short</c>. Without a filter, it asks which benchmarks to run.</param>
		/// <returns>0 once the benchmarks have run, or 1 in a Debug build, which runs none.</returns>
		public static int Main(string[] args)
		{
#if DEBUG
			Console.Error.WriteLine("Run the benchmarks in Release: dotnet run -c Release -p:Platform=x64 --project Tst/BlueDotBrigade.Weevil.Core-PerformanceTests");
			return 1;
#else
			// BenchmarkDotNet also refuses an assembly that references an unoptimized one, and DatenLokator, which only the tests use, ships unoptimized.
			// So its check is turned off, and the #if DEBUG above takes its place: this project, Core and Common are built in Release.
			IConfig config = DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator);
			BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

			return 0;
#endif
		}
	}
}
