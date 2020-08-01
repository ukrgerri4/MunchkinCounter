using Core.Models;
using Infrastracture.Models;
using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text;
using TcpMobile.Tcp.Models;

namespace Infrastracture.Interfaces.GameMunchkin
{
    public interface ILanServer
    {
        Subject<TcpEvent> TcpServerEventSubject { get; }
        void StartTcpServer(int port = 42420);
        void StopTcpServer();

        void StartUdpServer();
        void StopUdpServer();
        Result<int> SendMessage(string id, byte[] message);
        void BroadcastMessage(byte[] message);
        Result BeginSendMessage(string id, byte[] message);
    }
}
