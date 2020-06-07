using System;

namespace Infrastracture.Models
{
    public enum LogType: byte
    {
        Debug,
        Warning,
        Error
    }
    public class LogUnit
    {
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string Message { get; set; }
        public LogType Type { get; set; }
    }
}
