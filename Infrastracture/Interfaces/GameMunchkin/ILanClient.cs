using Core.Models;
using Infrastracture.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reactive.Subjects;
using System.Text;
using TcpMobile.Tcp.Models;

namespace Infrastracture.Interfaces.GameMunchkin
{
    public interface ILanClient
    {
        Subject<TcpEvent> PacketSubject { get; }
        Result Connect(IPAddress address, int port = 42420);
        void Disconnect();
        Result<int> SendMessage(byte[] message);
        void StartListeningBroadcast(int port = 42424);
        void StopListeningBroadcast();
    }
}
