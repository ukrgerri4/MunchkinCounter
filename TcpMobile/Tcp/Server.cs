using Core.Models;
using Infrastracture.Interfaces.GameMunchkin;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using TcpMobile.Tcp.Models;

namespace TcpMobile.Tcp
{
    public class Server: IGameServer
    {
        private readonly byte[] UDP_BROADCAST_MESSAGE = { 42 };
        public Subject<Packet> PacketSubject { get; set; } = new Subject<Packet>();

        private Subject<Socket> _udpSubject;

        public Dictionary<string, StateObject> —onfirmedConnections = new Dictionary<string, StateObject>();

        public delegate void ServerStartCallback();
        public delegate void ClientConnectCallback(string remoteIp);
        public delegate void ClientDisconnectCallback();
        public delegate void ReceiveDataCallback(Packet packet);

        public ServerStartCallback onServerStart { get; set; }
        public ClientConnectCallback onClientConnect { get; set; }
        public ClientDisconnectCallback onClientDisconnect { get; set; }
        public ReceiveDataCallback onReceiveData { get; set; }

        private Socket _mainTcpSocket;
        
        private Socket _mainUdpSocket;
        private IPEndPoint udpClientEP = new IPEndPoint(IPAddress.Broadcast, 42424);

        public bool IsListening
        {
            get
            {
                if (_mainTcpSocket == null)
                    return false;
                else
                    return _mainTcpSocket.IsBound;
            }
        }
        public void Start(int port = 42420)
        {
            try
            {
                Stop();

                _mainTcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _mainTcpSocket.Bind(new IPEndPoint(IPAddress.Any, port));
                _mainTcpSocket.Listen(100);
                _mainTcpSocket.BeginAccept(new AsyncCallback(OnReceiveConnection), _mainTcpSocket);

                onServerStart?.Invoke();
            }

            catch (SocketException se)
            {
                Console.WriteLine(se.Message);
            }
        }

        public void Stop()
        {
            if (IsListening)
                _mainTcpSocket.Close();
        }

        private void OnReceiveConnection(IAsyncResult asyncResult)
        {
            try
            {
                var listener = (Socket)asyncResult.AsyncState;
                var handler = listener.EndAccept(asyncResult);
                var stateObj = new StateObject(handler);

                onClientConnect?.Invoke(stateObj.workSocket.RemoteEndPoint.ToString());

                handler.BeginReceive(stateObj.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(OnDataReceived), stateObj);

                listener.BeginAccept(new AsyncCallback(OnReceiveConnection), listener);
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("OnClientConnection: Socket has been closed");
            }
            catch (SocketException se)
            {
                Console.WriteLine($"OnClientConnection: {se.Message}");
                onClientDisconnect?.Invoke();
            }
        }

        private void OnDataReceived(IAsyncResult asyncResult)
        {
            try
            {
                var stateObj = (StateObject)asyncResult.AsyncState;
                var handler = stateObj.workSocket;

                int bytesRead = handler.EndReceive(asyncResult);

                if (bytesRead > 0)
                {
                    var packet = new Packet(stateObj.buffer.Take(bytesRead).ToArray());
                    //onReceiveData?.Invoke(packet);
                    if (!stateObj.IdRecived)
                    {
                        if (packet.MessageType != Enums.MunchkinMessageType.GetId)
                        {
                            handler.BeginReceive(stateObj.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(OnDataReceived), stateObj);
                            return;
                        }

                        var id = Encoding.UTF8.GetString(packet.Buffer, 4, packet.Buffer[3]); // Ò 4-„Ó ·‡ÈÚ‡ Ë‰ÂÚ ID, ‚ 3-Ï ·‡ÈÚÂ ‰ÎËÌ‡ ID
                        stateObj.Id = id;
                        —onfirmedConnections.Add(id, stateObj);
                        handler.BeginReceive(stateObj.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(OnDataReceived), stateObj);
                        return;
                    }

                    packet.SenderId = stateObj.Id;
                    PacketSubject.OnNext(packet);
                }

                handler.BeginReceive(stateObj.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(OnDataReceived), stateObj);
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("OnDataReceived: Socket has been closed");
            }
            catch (SocketException se)
            {
                Console.WriteLine($"OnDataReceived: {se.Message}");
                onClientDisconnect?.Invoke();
            }
            catch (JsonReaderException jre)
            {
                Console.WriteLine($"OnDataReceived: {jre.Message}");
            }
            catch (Exception e)
            {
                var stateObj = (StateObject)asyncResult.AsyncState;
                var handler = stateObj.workSocket;
                if (handler.Connected)
                {
                    handler.BeginReceive(stateObj.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(OnDataReceived), stateObj);
                }
                Console.WriteLine($"OnDataReceived unexpected: {e.Message}");
            }
        }

        public Result<int> SendMessage(string id, byte[] message)
        {
            try
            {
                if (_mainTcpSocket == null)
                {
                    return Result.Fail<int>("Server soket is null.");
                }

                if (!_mainTcpSocket.Connected)
                {
                    return Result.Fail<int>("Server soket is not connected.");
                }

                if (!—onfirmedConnections.ContainsKey(id))
                {
                    return Result.Fail<int>($"Soket with id - [{id}] not found.");
                }

                var remoteStateObj = —onfirmedConnections[id];

                if (remoteStateObj.workSocket == null || remoteStateObj.workSocket.Connected)
                {
                    return Result.Fail<int>("Remote soket is not connected.");
                }

                var bytesSent = remoteStateObj.workSocket.Send(message);
                return Result.Ok(bytesSent);
            }
            catch (Exception e)
            {
                Console.WriteLine($"SendMessage unexpected: {e.Message}");
                return Result.Fail<int>($"Unexpected: {e.Message}");
            }
        }

        public void StartBroadcast()
        {
            try
            {
                StopBroadcast();

                _udpSubject = new Subject<Socket>();

                _mainUdpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _mainUdpSocket.EnableBroadcast = true;
                _mainUdpSocket.Bind(new IPEndPoint(IPAddress.Any, 0));

                _udpSubject
                    .AsObservable()
                    .Throttle(TimeSpan.FromSeconds(1))
                    .Finally(() =>
                    {
                        Console.WriteLine("_udpSubject closed.");
                    })
                    .Subscribe(socket =>
                    {
                        socket.BeginSendTo(UDP_BROADCAST_MESSAGE, 0, 1, SocketFlags.None, udpClientEP, new AsyncCallback(SendCallback), socket);
                    });

                if (_udpSubject != null && !_udpSubject.IsDisposed)
                {
                    _udpSubject.OnNext(_mainUdpSocket);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"StartBroadcast unexpected: {e.Message}");
            }
        }

        private void SendCallback(IAsyncResult asyncResult)
        {
            if (_udpSubject != null && !_udpSubject.IsDisposed)
            {
                _udpSubject.OnNext((Socket)asyncResult.AsyncState);
            }
        }

        public void StopBroadcast()
        {
            try
            {
                if (_udpSubject != null && !_udpSubject.IsDisposed)
                {
                    _udpSubject.OnCompleted();
                    _udpSubject.Dispose();
                }

               _mainUdpSocket?.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine($"OnDataReceived unexpected: {e.Message}");
                _mainUdpSocket?.Close();
            }
        }
    }
}
