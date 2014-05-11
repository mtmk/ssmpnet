using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace Ssmpnet.Tester2
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            var page = XamlReader.Load(Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(Program), "page.xaml")) as Page;
            page.DataContext = new TesterViewModel();

            var window = new Window
                         {
                             SizeToContent = SizeToContent.WidthAndHeight,
                             Content = page
                         };
            window.Show();
            
            var application = new Application();
            application.Run(window);
        }
    }

    class TesterViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _text1;

        public string Text1
        {
            get { return _text1; }
            set { _text1 = value; OnPropertyChanged("Text1"); }
        }

        void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public ICommand Go
        {
            get
            {
                return new SimpleCommand(() =>
                                           {
                                               Console.WriteLine("GO..." + _text1);
                                           });
            }
        }
    }

    class SimpleCommand : ICommand
    {
        private readonly Action _action;

        public SimpleCommand(Action action)
        {
            _action = action;
        }

        public void Execute(object parameter)
        {
            _action();
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
    }
}
