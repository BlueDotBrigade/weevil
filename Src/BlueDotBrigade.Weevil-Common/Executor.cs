namespace BlueDotBrigade.Weevil
{
	using System;

	/// <summary>
	/// Facilitates the creation of a <see cref="Executor"/> class, by leveraging CSharp's ability
	/// to imply the generic type based on the given parameters.
	/// </summary>
	/// <remarks>
	/// Implementation is based on Microsoft's <see cref="System.Windows.Input.ICommand"/> interface and <see cref="System.Tuple"/> class.
	/// </remarks>
	/// <seealso href="https://github.com/dotnet/roslyn/issues/2319">Infer generic type arguments from constructor arguments</seealso>
	public static class Executor
	{
		public static Executor<I> Create<I>(I instance, Func<I, bool> canExecuteFunc, Action<I> executeAction)
		{
			return new Executor<I>(instance, canExecuteFunc, executeAction);
		}
	}
}
