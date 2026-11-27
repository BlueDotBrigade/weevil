namespace BlueDotBrigade.Weevil
{
	using System;

	public class Executor<TInstance> : IExecute
	{
		private readonly Func<TInstance, bool> _canExecutionFunc;
		private readonly Action<TInstance> _executionAction;
		private readonly TInstance _instance;

		public Executor(TInstance instance, Func<TInstance, bool> canExecutionFunc, Action<TInstance> executionAction)
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