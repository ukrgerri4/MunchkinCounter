using System.ComponentModel;
using System.Net;

namespace Infrastracture.Models
{
    public class MunchkinHost: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string prop = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));


        private string _id;
        public string Id
        { 
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged(nameof(Id));
                }
            }
        }

        private IPAddress _ipAddress;
        public IPAddress IpAddress
        {
            get => _ipAddress;
            set
            {
                if (_ipAddress != value)
                {
                    _ipAddress = value;
                    OnPropertyChanged(nameof(IpAddress));
                }
            }
        }
        
        private string _name = "Game 1";
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        
        private byte _capacity = 6;
        public byte Capacity
        {
            get => _capacity;
            set
            {
                if (_capacity != value)
                {
                    _capacity = value > 0 ? value : (byte)1;
                    OnPropertyChanged(nameof(Capacity));
                }
            }
        }
        
        private byte _fullness = 0;
        public byte Fullness
        { 
            get => _fullness;
            set
            {
                if (_fullness != value)
                {
                    _fullness = value >= 0 ? value : (byte)0;
                    OnPropertyChanged(nameof(Fullness));
                }
            }
        }
    }
}
