using System.Collections.Generic;
using System.ComponentModel;

namespace GameMunchkin.Models
{
    public class Player : INotifyPropertyChanged
    {
        private readonly Dictionary<byte, string> _colors;

        private string id;
        private string name;
        private byte level;
        private byte modifiers;

        public event PropertyChangedEventHandler PropertyChanged;
        public string Id
        {
            get => id;
            set
            {
                if (id != value)
                {
                    id = value;
                    OnPropertyChanged("Id");
                }
            }
        }
        public string Name
        {
            get => name;
            set
            {
                if (name != value)
                {
                    name = value;
                    OnPropertyChanged("Name");
                }
            }
        }
        public byte Level
        {
            get => level;
            set
            {
                if (level != value)
                {
                    level = value;
                    OnPropertyChanged("Level");
                    OnPropertyChanged("Power");
                    OnPropertyChanged("Color");
                }
            }
        }
        public byte Modifiers
        { 
            get => modifiers;
            set
            {
                if (modifiers != value)
                {
                    modifiers = value;
                    OnPropertyChanged("Modifiers");
                    OnPropertyChanged("Power");
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

        public void OnPropertyChanged(string prop = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
