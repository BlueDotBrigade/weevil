namespace BlueDotBrigade.Weevil.Gui.Input
{
	using System;
	using System.Diagnostics;
	using System.Runtime.CompilerServices;
	using System.Windows;
	using System.Windows.Input;
	using BlueDotBrigade.Weevil.Diagnostics;
	using BlueDotBrigade.Weevil.Filter.Expressions;
	using Prism.Commands;

	/// <summary>
	/// Encapsulates an <see cref="ICommand"/> that will be invoked after it has been written to the application's log file.
	/// </summary>
	/// <seealso href="https://joshsmithonwpf.wordpress.com/2007/10/25/logging-routed-commands/">Josh Smith: Logging Routed Commands</seealso>
	/// <seealso href="https://www.c-sharpcorner.com/UploadFile/e06010/wpf-icommand-in-mvvm/">Encapsulating an ICommand</seealso>
	public class UiBoundCommand : ICommand
	{
		private static readonly string UnknownCommand = "Unknown";

		private static readonly Func<bool> CanAlwaysExecuteMethod = () => true;

		private readonly DelegateCommand _command;
		private readonly string _commandName;

		/// <summary>
		/// Encapsulates an <see cref="ICommand"/> that will be invoked after it has been written to the application's log file.
		/// </summary>
		/// <param name="executeMethod">A delegate that refers to the code to be executed.</param>
		/// <param name="commandName">The name of the command to be logged.</param>
		/// <seealso href="https://joshsmithonwpf.wordpress.com/2007/10/25/logging-routed-commands/">Josh Smith: Logging Routed Commands</seealso>
		/// <seealso href="https://www.c-sharpcorner.com/UploadFile/e06010/wpf-icommand-in-mvvm/">Encapsulating an ICommand</seealso>

		public UiBoundCommand(Action executeMethod, [CallerMemberName] string commandName = "")
		: this(executeMethod, CanAlwaysExecuteMethod, commandName)
		{
			// nothing to do
		}

		/// <summary>
		/// Encapsulates an <see cref="ICommand"/> that will be invoked after it has been written to the application's log file.
		/// </summary>
		/// <param name="canExecuteMethod">Returning a valid of <see langword="true"/> indicates that the <see cref="executeMethod"/> can be invoked.</param>
		/// <param name="executeMethod">A delegate that refers to the code to be executed.</param>
		/// <param name="commandName">The name of the command to be logged.</param>
		/// <seealso href="https://joshsmithonwpf.wordpress.com/2007/10/25/logging-routed-commands/">Josh Smith: Logging Routed Commands</seealso>
		/// <seealso href="https://www.c-sharpcorner.com/UploadFile/e06010/wpf-icommand-in-mvvm/">Encapsulating an ICommand</seealso>
		public UiBoundCommand(Action executeMethod, Func<bool> canExecuteMethod, [CallerMemberName] string commandName = "")
		{
			_commandName = !string.IsNullOrWhiteSpace(commandName) ? commandName : UnknownCommand;

			Debug.Assert(_commandName != UnknownCommand, "A name that reflects the command being executed, is required.");
			Debug.Assert(_commandName != ".ctor", "A name that reflects the command being executed, is required.");

			_command = new DelegateCommand(() =>
			{
				try
				{
					executeMethod.Invoke();
				}
				catch (InvalidExpressionException e)
				{
					var message =
						$"Unable to perform the requested operation. Reason: {e.Message}";

					Log.Default.Write(
						LogSeverityType.Information,
						e,
						message);
					MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
				catch (Exception e)
				{
					var message =
						$"Unable to perform the requested operation. CommandName={_commandName}";
					Log.Default.Write(
						LogSeverityType.Information,
						e,
						message);
					MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				}
			}, canExecuteMethod);
		}

		public event EventHandler CanExecuteChanged
		{
			add { CommandManager.RequerySuggested += value; }
			remove { CommandManager.RequerySuggested -= value; }
		}

		bool ICommand.CanExecute(object parameter)
		{
			var command = _command as ICommand;
			return command.CanExecute(parameter);
		}

		public bool CanExecute()
		{
			return _command.CanExecute();
		}

		void ICommand.Execute(object parameter)
		{
			Log.Default.Write(
				LogSeverityType.Information,
				$"A command bound to the user interface is executing. CommandName={_commandName}");

			var command = _command as ICommand;
			command.Execute(parameter);
		}

		public void Execute()
		{
			Log.Default.Write(
				LogSeverityType.Information,
				$"A command bound to the user interface is executing. CommandName={_commandName}");

			_command.Execute();
		}

		public void RaiseCanExecuteChanged()
		{
			_command.RaiseCanExecuteChanged();
		}
	}
}
