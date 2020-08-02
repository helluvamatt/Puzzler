using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Puzzler.ViewModels
{
	public class DelegateCommand : ICommand
	{
		private readonly Action _Action;
		private readonly Func<bool> _CanExecute;

		public DelegateCommand(Action action, Func<bool> canExecute = null)
		{
			_Action = action ?? throw new ArgumentNullException(nameof(action));
			_CanExecute = canExecute;
		}

		public event EventHandler CanExecuteChanged
		{
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}

		public bool CanExecute(object parameter) => _CanExecute?.Invoke() ?? true;

		public void Execute(object parameter)
		{
			_Action.Invoke();
		}
	}
}
