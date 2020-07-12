using System.Collections.Generic;
using System.ComponentModel;

namespace GameMunchkin.Models
{
    public class Player : INotifyPropertyChanged
    {
        private readonly Dictionary<byte, string> _colors;

        private string _id;
        private string _name;
        private byte _sex; // 0 - female, 1 - male
        private byte _level;
        private byte _modifiers;

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
                    OnPropertyChanged(nameof(Color));
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

        public string Color
        {
            get => _colors[Level];
        }

        public string SexIcon
        {
            get => _sex == 1 ? "male64.png" : "female64.png";
        }

        public Player()
        {
            Level = 1;
            Modifiers = 0;

            _colors = new Dictionary<byte, string>
            {
                { 1, "#57bb8a"},
                { 2, "#73b87e"},
                { 3, "#94bd77"},
                { 4, "#b0be6e"},
                { 5, "#d4c86a"},
                { 6, "#f5ce62"},
                { 7, "#e9b861"},
                { 8, "#ecac67"},
                { 9, "#e5926b"},
                { 10, "#dd776e"}
            };
        }

    }
}
