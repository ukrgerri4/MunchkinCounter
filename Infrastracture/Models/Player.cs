using Core.Utils;
using System.ComponentModel;
using System.Windows.Input;
using Xamarin.Forms;

namespace Infrastracture.Models
{
    public class Player : INotifyPropertyChanged
    {
        private string _id;
        private string _name;
        private byte _sex; // 0 - female, 1 - male
        private byte _level;
        private byte _modifiers;

        public ICommand IncreaseLevel { get; set; }
        public ICommand DecreaseLevel { get; set; }
        public ICommand IncreaseModifiers { get; set; }
        public ICommand DecreaseModifiers { get; set; }
        public ICommand ToggleSex { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string prop = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
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

        public byte Sex
        {
            get => _sex;
            set
            {
                if (_sex != value)
                {
                    _sex = value;
                    OnPropertyChanged(nameof(Sex));
                    OnPropertyChanged(nameof(SexIcon));
                }
            }
        }

        public byte Level
        {
            get => _level;
            set
            {
                if (_level != value)
                {
                    _level = value;
                    OnPropertyChanged(nameof(Level));
                    OnPropertyChanged(nameof(Power));
                }
            }
        }
        public byte Modifiers
        {
            get => _modifiers;
            set
            {
                if (_modifiers != value)
                {
                    _modifiers = value;
                    OnPropertyChanged(nameof(Modifiers));
                    OnPropertyChanged(nameof(Power));
                }
            }
        }

        public int Power
        {
            get => Level + Modifiers;
        }

        public string SexIcon
        {
            get => _sex == 1 ? FontAwesomeIcons.Mars : FontAwesomeIcons.Venus;
        }

        public Player()
        {
            Level = 1;
            Modifiers = 0;

            IncreaseLevel = new Command(
                () =>
                {
                    if (Level < 10)
                    {
                        Level++;
                        RefreshLevelCanExecutes();
                    }
                },
                () => { return Level < 10; }
            );

            DecreaseLevel = new Command(
                () =>
                {
                    if (Level > 1)
                    {
                        Level--;
                        RefreshLevelCanExecutes();
                    }
                },
                () => { return Level > 1; }
            );

            IncreaseModifiers = new Command(
                () =>
                {
                    if (Modifiers < 255)
                    {
                        Modifiers++;
                        RefreshModifiersCanExecutes();
                    }
                },
                () => { return Modifiers < 255; }
            );

            DecreaseModifiers = new Command(
                () =>
                {
                    if (Modifiers > 0)
                    {
                        Modifiers--;
                        RefreshModifiersCanExecutes();
                    }
                },
                () => { return Modifiers > 0; }
            );

            ToggleSex = new Command(
                () =>
                {
                    Sex = Sex == 1 ? (byte)0 : (byte)1;
                }
            );
        }

        private void RefreshLevelCanExecutes()
        {
            (IncreaseLevel as Command).ChangeCanExecute();
            (DecreaseLevel as Command).ChangeCanExecute();
        }

        private void RefreshModifiersCanExecutes()
        {
            (IncreaseModifiers as Command).ChangeCanExecute();
            (DecreaseModifiers as Command).ChangeCanExecute();
        }

        public void ResetLevel()
        {
            Level = 1;
            RefreshLevelCanExecutes();
        }

        public void ResetModifyers()
        {
            Modifiers = 0;
            RefreshModifiersCanExecutes();
        }
    }
}
