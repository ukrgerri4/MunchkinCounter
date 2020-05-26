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
        void Start(int port);
        void Stop();

        void StartBroadcast();
        void StopBroadcast();
    }
}
