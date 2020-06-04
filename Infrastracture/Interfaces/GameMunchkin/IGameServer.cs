using Core.Models;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text;
using TcpMobile.Tcp.Models;

namespace Infrastracture.Interfaces.GameMunchkin
{
    public interface IGameServer
    {
        Subject<Packet> PacketSubject { get; }
        void Start(int port = 42420);
        void Stop();

        void StartBroadcast();
        void StopBroadcast();
        Result<int> SendMessage(string id, byte[] message);
    }
}
