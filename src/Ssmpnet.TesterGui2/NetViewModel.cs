using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Management.Instrumentation;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Ssmpnet.TesterGui2
{
    public class NetViewModel : INotifyPropertyChanged
    {
        private volatile string _publisherInfo;
        public string PublisherInfo
        {
            get
            {
                Debug.WriteLine("Gn:" + Thread.CurrentThread.ManagedThreadId);
                Debug.WriteLine("Gt:" + Task.CurrentId);
                return _publisherInfo;
            }
            set { _publisherInfo = value; OnPropertyChanged(); }
        }

        private volatile string _subscriberInfo;
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
                       // for (double i = 0; i < Math.PI; i += .1)
                       // {
                       //     Thread.Sleep(100);
                        PublisherInfo = "X";//new string('=', (int)(Math.Sin(i) * 10));
                            Debug.WriteLine("Bn:" + Thread.CurrentThread.ManagedThreadId);
                            Debug.WriteLine("Bt:" + Task.CurrentId);
                      //  }
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