using System;
using System.Diagnostics;
using System.Threading;
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
            Debug.WriteLine("Cn:" + Thread.CurrentThread.ManagedThreadId);
            Debug.WriteLine("Cn:" + Thread.CurrentThread.Name);
            Debug.WriteLine("Ct:" + Task.CurrentId);

            Task.Factory.StartNew(_action);
        }

        public event EventHandler CanExecuteChanged;
    }
}