using TcpMobile.Tcp.Models;

namespace Infrastracture.Models
{
    public enum TcpEventType
    {
        ServerStarted,
        ClientConnected,
        ClientDisconnect,
        ReceiveData
    }

    public class TcpEvent
    {
        public TcpEventType Type { get; set; }
        public object Data { get; set; }
    }
}
