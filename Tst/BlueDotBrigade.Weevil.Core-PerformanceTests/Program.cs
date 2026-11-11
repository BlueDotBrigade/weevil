namespace BlueDotBrigade.Weevil
{
	using BenchmarkDotNet.Configs;
	using BenchmarkDotNet.Running;

	/// <summary>
	/// Runs the benchmarks the command line chooses, for example <c>--filter *Filter*</c>, or asks which to run (#1177).
	/// </summary>
	/// <remarks>
	/// <c>dotnet test</c> runs this project's tests, and never calls this method.
	/// </remarks>
	public static class Program
	{
		public static int Main(string[] args)
		{
#if DEBUG
			Console.Error.WriteLine("Run the benchmarks in Release: dotnet run -c Release -p:Platform=x64 --project Tst/BlueDotBrigade.Weevil.Core-PerformanceTests");
			return 1;
#else
			// BenchmarkDotNet refuses an assembly that references an unoptimized one, and DatenLokator, which only the tests use, ships unoptimized.
			// This project, Core and Common are built in Release, as the check above makes sure.
			IConfig config = DefaultConfig.Instance.WithOptions(ConfigOptions.DisableOptimizationsValidator);
			BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

			return 0;
#endif
		}
	}
}
