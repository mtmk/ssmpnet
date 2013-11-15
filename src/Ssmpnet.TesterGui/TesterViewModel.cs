using System.Collections.ObjectModel;
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
            set
            {
                _rate = value;
                RaisePropertyChanged("Rate");
            }
        }

        public ICommand ChangeRate
        {
            get { return new RelayCommand(() => Rate = "w"); }
        }

        public ObservableCollection<TesterItemViewModel> TesterItems { get; set; }
    }

    public class TesterItemViewModel : ObservableObject
    {
        private string _location;

        public string Location
        {
            get { return _location; }
            set { _location = value; RaisePropertyChanged("Location"); }
        }
    }
}