using Infrastracture.Models;
using System.Collections.ObjectModel;

namespace Infrastracture.Interfaces
{
    public interface IGameLogger
    {
        void Debug(string message);
        void Error(string message);
        ObservableCollection<LogUnit> GetHistory();
        void Warning(string message);
    }
}
