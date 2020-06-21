using System;
using System.ComponentModel;

namespace Infrastracture.Models
{
    public enum LogType: byte
    {
        Debug,
        Warning,
        Error
    }
    public class LogUnit: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string prop = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string Message { get; set; }
        public LogType Type { get; set; }

        private int _duplicateMessagesCounter = 0;
        public int DuplicateMessagesCounter
        {
            get => _duplicateMessagesCounter; 
            set
            {
                if (_duplicateMessagesCounter != value)
                {
                    _duplicateMessagesCounter = value;
                    OnPropertyChanged(nameof(DuplicateMessagesCounter));
                    OnPropertyChanged(nameof(ShowDuplicateMessagesCounter));
                }
            } 
        }

        public bool ShowDuplicateMessagesCounter
        {
            get => _duplicateMessagesCounter > 0;
        }

        public string Color
        {
            get
            {
                switch (Type)
                {
                    case LogType.Warning:
                        return "#FFCC00";
                    case LogType.Error:
                        return "#FF9966";
                    default:
                        return "#FFFFFF";
                }
            }
        }
    }
}
