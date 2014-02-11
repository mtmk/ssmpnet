using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Ssmpnet.TesterGui
{
    public class Tl : TraceListener
    {

        public override void Write(string message)
        {
            Debug.Write(message);
        }

        public override void WriteLine(string message)
        {
            Debug.WriteLine(message);
        }
    }
    public class TesterViewModel : ObservableObject
    {
        private static readonly TraceSource TraceSource = new TraceSource("Ssmpnet");

        public TesterViewModel()
        {
            TraceSource.Listeners.Add(new Tl { Filter = new EventTypeFilter(SourceLevels.All) });
            TraceSource.Switch = new SourceSwitch("x") { Level = SourceLevels.All };

            TesterItems = new ObservableCollection<TesterItemViewModel>(new[]
                {
                    new TesterItemViewModel{Location = "xxx"}, 
                    new TesterItemViewModel{Location = "yyy"}, 
                });
        }

        private int _pubRateCounter;
        private volatile string _pubRate = "0";
        public string PubRate
        {
            get { return _pubRate; }
            set { _pubRate = value; RaisePropertyChanged(() => PubRate); }
        }

        private int _subRateCounter;
        private volatile string _subRate = "0";
        public string SubRate
        {
            get { return _subRate; }
            set { _subRate = value; RaisePropertyChanged(() => SubRate); }
        }

        private string _subPort = "127.0.0.1:56987";
        public string SubPort
        {
            get { return _subPort; }
            set { _subPort = value; RaisePropertyChanged(() => SubPort); }
        }

        private string _pubPort = "127.0.0.1:56987";
        public string PubPort
        {
            get { return _pubPort; }
            set { _pubPort = value; RaisePropertyChanged(() => PubPort); }
        }

        private string _subCmdText = "Start subscriber";
        public string SubCmdText
        {
            get { return _subCmdText; }
            set { _subCmdText = value; RaisePropertyChanged(() => SubCmdText); }
        }

        private bool _subEnabled = true;
        public bool SubEnabled
        {
            get { return _subEnabled; }
            set { _subEnabled = value; RaisePropertyChanged(() => SubEnabled); }
        }

        private volatile string _pubCmdText = "Start publisher";
        public string PubCmdText
        {
            get { return _pubCmdText; }
            set { _pubCmdText = value; RaisePropertyChanged(() => PubCmdText); }
        }

        private bool _pubEnabled = true;
        public bool PubEnabled
        {
            get { return _pubEnabled; }
            set { _pubEnabled = value; RaisePropertyChanged(() => PubEnabled); }
        }

        private Task _tPub;
        private readonly ManualResetEvent _pubEvent = new ManualResetEvent(false);
        public ICommand StartPub
        {
            get
            {
                return new RelayCommand(() =>
                    {
                        if (_pubCmdText.StartsWith("Stop"))
                        {
                            PubEnabled = false;
                            PubCmdText = "Stopping..";
                            _pubEvent.Set();
                            _tPub.Wait();
                            PubCmdText = "Start publisher";
                            PubEnabled = true;
                            return;
                        }

                        PubEnabled = false;
                        PubCmdText = "Starting..";

                        var publisherToken = PublisherSocket.Start(new IPEndPoint(IPAddress.Parse(PubPort.Split(':')[0]), int.Parse(PubPort.Split(':')[1])));

                        _tPub = Task.Factory.StartNew(() =>
                            {
                                Thread.Sleep(1000);
                                PubCmdText = "Stop publisher";
                                PubEnabled = true;
                                _pubEvent.Reset();
                                var message = new byte[1024];
                                var sw = new Stopwatch();
                                sw.Start();
                                int lastCount = 0;
                                string rate = "";
                                while (!_pubEvent.WaitOne(0))
                                {
                                    publisherToken.Publish(message);
                                    var r = Interlocked.Increment(ref _pubRateCounter);

                                    if (sw.ElapsedMilliseconds > 5000)
                                    {
                                        lastCount = r;
                                        sw.Restart();
                                    }

                                    var elapsedMilliseconds = sw.ElapsedMilliseconds;
                                    if (elapsedMilliseconds == 0) elapsedMilliseconds = 1;
                                    rate = ((r - lastCount) / elapsedMilliseconds).ToString("0.00");

                                    PubRate = r.ToString() + " " + rate;
                                }
                                publisherToken.Close();
                            });
                    });
            }
        }

        private SubscriberToken _subscriberToken;
        public ICommand StartSub
        {
            get
            {
                return new RelayCommand(() =>
                {
                    if (_subCmdText.StartsWith("Stop"))
                    {
                        SubEnabled = false;
                        _subscriberToken.Close();
                        SubCmdText = "Start subscriber";
                        SubEnabled = true;
                        return;
                    }

                    SubEnabled = false;
                    SubCmdText = "Starting..";

                    _subscriberToken = SubscriberSocket.Start(new IPEndPoint(IPAddress.Parse(SubPort.Split(':')[0]), int.Parse(SubPort.Split(':')[1])),
                                                                (m, o, s) =>
                                                                {
                                                                    var i = Interlocked.Increment(ref _subRateCounter);
                                                                    SubRate = i.ToString();
                                                                });

                    Task.Factory.StartNew(() =>
                        {
                            Thread.Sleep(1000);
                            SubCmdText = "Stop subscriber";
                            SubEnabled = true;
                        });
                });
            }
        }

        public ObservableCollection<TesterItemViewModel> TesterItems { get; set; }
    }

    public class TesterItemViewModel : ObservableObject
    {
        private string _location;

        public string Location
        {
            get { return _location; }
            set { _location = value; RaisePropertyChanged(() => Location); }
        }
    }
}