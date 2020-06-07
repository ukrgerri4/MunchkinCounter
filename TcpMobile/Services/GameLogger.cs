using Infrastracture.Interfaces;
using Infrastracture.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace TcpMobile.Services
{
    public class GameLogger: IGameLogger
    {
        private ObservableCollection<LogUnit> _history = new ObservableCollection<LogUnit>();

        public void Debug(string message)
        {
            _history.Add(new LogUnit { Message = message, Type = LogType.Debug});
        }

        public void Warning(string message)
        {
            _history.Add(new LogUnit { Message = message, Type = LogType.Warning });
        }

        public void Error(string message)
        {
            _history.Add(new LogUnit { Message = message, Type = LogType.Error });
        }

        public ObservableCollection<LogUnit> GetHistory() => _history;
    }
}
