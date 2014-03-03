using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Ssmpnet.TesterGui2
{
    public class SimpleCommand : ICommand
    {
        private readonly Action _action;

        public SimpleCommand(Action action)
        {
            _action = action;
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            Task.Factory.StartNew(_action);
        }

        public event EventHandler CanExecuteChanged;
    }
}