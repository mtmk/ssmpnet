using System;
using System.ComponentModel;
using System.Management.Instrumentation;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Ssmpnet.TesterGui2
{
    public class NetViewModel : INotifyPropertyChanged
    {
        private string _publisherInfo;
        public string PublisherInfo
        {
            get { return _publisherInfo; }
            set { _publisherInfo = value; OnPropertyChanged(); }
        }

        private string _subscriberInfo;
        public string SubscriberInfo
        {
            get { return _subscriberInfo; }
            set { _subscriberInfo = value; OnPropertyChanged(); }
        }

        public ICommand StartPublisher
        {
            get
            {
                var sync = new object();
                return new SimpleCommand(() =>
                {
                    if (!Monitor.TryEnter(sync)) return;
                    try
                    {
                        for (double i = 0; i < Math.PI; i += .1)
                        {
                            Thread.Sleep(100);
                            PublisherInfo = new string('=', (int)(Math.Sin(i) * 10));
                        }
                    }
                    finally
                    {
                        Monitor.Exit(sync);
                    }
                });
            }
        }

        public ICommand StartSubscriber
        {
            get { return new SimpleCommand(() => { SubscriberInfo = "xx"; }); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}