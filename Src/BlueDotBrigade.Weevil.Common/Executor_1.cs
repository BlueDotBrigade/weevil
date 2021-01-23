namespace BlueDotBrigade.Weevil
{
	using System;

	public class Executor<I> : IExecute
	{
		private readonly Func<I, bool> _canExecutionFunc;
		private readonly Action<I> _executionAction;
		private readonly I _instance;

		public Executor(I instance, Func<I, bool> canExecutionFunc, Action<I> executionAction)
		{
			_instance = instance;
			_canExecutionFunc = canExecutionFunc;
			_executionAction = executionAction;
		}

		public bool CanExecute()
		{
			return _canExecutionFunc(_instance);
		}

		public void Execute()
		{
			_executionAction(_instance);
		}
	}
}