using System;
using System.Windows.Input;

namespace OD_Trade_Mission_Tracker.Utils
{
    public class SimpleCommand : ICommand
    {
        public event EventHandler<object> Executed;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            Executed?.Invoke(this, parameter);
        }

        public event EventHandler CanExecuteChanged;
    }
}
