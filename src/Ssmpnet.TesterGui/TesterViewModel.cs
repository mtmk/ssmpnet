using System.Collections.ObjectModel;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Ssmpnet.TesterGui
{
    public class TesterViewModel : ObservableObject
    {
        public TesterViewModel()
        {
            TesterItems = new ObservableCollection<TesterItemViewModel>(new[]
                {
                    new TesterItemViewModel{Location = "xxx"}, 
                    new TesterItemViewModel{Location = "yyy"}, 
                });
        }

        private string _rate = "11";
        public string Rate
        {
            get { return _rate; }
            set { _rate = value; RaisePropertyChanged(() => Rate); }
        }
        
        private string _pubPort = "11";
        public string Rate
        {
            get { return _rate; }
            set { _rate = value; RaisePropertyChanged(() => Rate); }
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

        private string _pubCmdText = "Start publisher";
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

        public ICommand ChangeRate
        {
            get { return new RelayCommand(() => Rate = "w"); }
        }

        private Task _tPub;
        public ICommand StartPub
        {
            get
            {
                return new RelayCommand(() =>
                    {
                        PubEnabled = false;

                        var publisherToken = PublisherSocket.Start(new IPEndPoint(IPAddress.Any, int.Parse(PubPort)));

                        _tPub = Task.Factory.StartNew(() =>
                            {
                                PubCmdText = "Starting..";
                                Thread.Sleep(2000);
                                PubCmdText = "Stop publisher";
                                PubEnabled = true;
                                for (int i = 0; i < 10; i++)
                                {
                                    Rate = i.ToString();
                                    Thread.Sleep(1000);
                                }
                            });
                    });
            }
        }

        private Task _tSub;
        public ICommand StartSub
        {
            get
            {
                return new RelayCommand(() =>
                {
                    SubEnabled = false;
                    _tSub = Task.Factory.StartNew(() =>
                    {
                        SubCmdText = "Starting..";
                        Thread.Sleep(2000);
                        SubCmdText = "Stop subscriber";
                        SubEnabled = true;
                        for (int i = 0; i < 10; i++)
                        {
                            Rate = i.ToString();
                            Thread.Sleep(1000);
                        }
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