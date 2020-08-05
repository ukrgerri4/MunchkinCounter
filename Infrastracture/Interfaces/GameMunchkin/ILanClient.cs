using Core.Models;
using Infrastracture.Models;
using System.Net;
using System.Reactive.Subjects;

namespace Infrastracture.Interfaces.GameMunchkin
{
    public interface ILanClient
    {
        Subject<TcpEvent> TcpClientEventSubject { get; }

        Result Connect(IPAddress address, int port = 42420);
        void Disconnect();
        void StartListeningBroadcast(int port = 42424);
        void StopListeningBroadcast();
        Result BeginSendMessage(byte[] message);
    }
}
