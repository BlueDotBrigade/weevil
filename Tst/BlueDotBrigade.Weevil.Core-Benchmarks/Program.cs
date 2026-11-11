namespace BlueDotBrigade.Weevil
{
	using BenchmarkDotNet.Running;

	/// <summary>
	/// Runs the benchmarks the command line chooses, for example <c>--filter *Filter*</c>, or asks which to run (#1177).
	/// </summary>
	public static class Program
	{
		public static void Main(string[] args)
		{
			BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
		}
	}
}
