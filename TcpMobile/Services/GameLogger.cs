using Infrastracture.Interfaces;
using Infrastracture.Models;
using System;
using System.Collections.ObjectModel;

namespace TcpMobile.Services
{
    public class GameLogger: IGameLogger
    {
        private ObservableCollection<LogUnit> _history = new ObservableCollection<LogUnit>();
        
        private LogUnit _lastLogUnit = null;

        public void Debug(string message)
        {
            WriteToHistory(message, LogType.Debug);
        }

        public void Warning(string message)
        {
            WriteToHistory(message, LogType.Error);
        }

        public void Error(string message)
        {
            WriteToHistory(message, LogType.Error);
        }

        public ObservableCollection<LogUnit> GetHistory() => _history;

        private void WriteToHistory(string message, LogType type)
        {
            if (IsDuplicateMessages(message))
            {
                _lastLogUnit.DuplicateMessagesCounter++;
                _lastLogUnit.Date = DateTime.UtcNow;
                return;
            }

            var logUnit = new LogUnit { Message = message, Type = type };
            _lastLogUnit = logUnit;
            _history.Add(logUnit);
        }

        private bool IsDuplicateMessages(string message)
        {
            if (_lastLogUnit == null) { return false; }

            return _lastLogUnit.Message.Equals(message);
        }
    }
}
