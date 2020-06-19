using TcpMobile.Tcp.Models;

namespace Infrastracture.Models
{
    public enum TcpEventType
    {
        ServerStart,
        ClientConnect,
        ClientDisconnect,
        ReceiveData
    }

    public class TcpEvent
    {
        public TcpEventType Type { get; set; }
        public Packet Data { get; set; }
    }
}
