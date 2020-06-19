using Core.Models;
using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using Infrastracture.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using TcpMobile.Tcp.Models;

namespace TcpMobile.Tcp
{
    public class LanServer: ILanServer
    {
        private readonly IGameLogger _gameLogger;

        private readonly byte[] UDP_BROADCAST_MESSAGE = { 42 };

        public Subject<TcpEvent> PacketSubject { get; set; } = new Subject<TcpEvent>();

        private Subject<Socket> _udpSubject;

        public Dictionary<string, StateObject> ÑonfirmedConnections = new Dictionary<string, StateObject>();

        public delegate void ServerStartCallback();
        public delegate void ClientConnectCallback(string remoteIp);
        public delegate void ClientDisconnectCallback();
        public delegate void ReceiveDataCallback(Packet packet);

        private Socket _mainTcpSocket;
        
        private Socket _mainUdpSocket;
        private IPEndPoint udpClientEP = new IPEndPoint(IPAddress.Broadcast, 42424);

        //private byte[] _broadcastMessage = new byte[] { 3, 0, 1 };

        public LanServer(IGameLogger gameLogger)
        {
            _gameLogger = gameLogger;
        }

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
        public void StartTcpServer(int port = 42420)
        {
            try
            {
                StopTcpServer();

                _mainTcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _mainTcpSocket.Bind(new IPEndPoint(IPAddress.Any, port));
                _mainTcpSocket.Listen(100);

                _gameLogger.Debug($"Start server: [{_mainTcpSocket.LocalEndPoint}]");

                _mainTcpSocket.BeginAccept(new AsyncCallback(OnReceiveConnection), _mainTcpSocket);

                PacketSubject?.OnNext(new TcpEvent { Type = TcpEventType.ServerStart });
            }

            catch (SocketException se)
            {
                _gameLogger.Debug(se.Message);
            }
        }

        public void StopTcpServer()
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

                //onClientConnect?.Invoke(stateObj.workSocket.RemoteEndPoint.ToString());

                ÑonfirmedConnections.Add(stateObj.Id, stateObj);
                handler.BeginReceive(stateObj.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(OnDataReceived), stateObj);

                listener.BeginAccept(new AsyncCallback(OnReceiveConnection), listener);
            }
            catch (ObjectDisposedException)
            {
                _gameLogger.Debug("OnClientConnection: Socket has been closed");
            }
            catch (SocketException se)
            {
                _gameLogger.Debug($"OnClientConnection: {se.Message}");
                PacketSubject?.OnNext(new TcpEvent { Type = TcpEventType.ClientDisconnect });
            }
        }

        private void OnDataReceived(IAsyncResult asyncResult)
        {
            try
            {
                var stateObj = (StateObject)asyncResult.AsyncState;
                var handler = stateObj.workSocket;

                if (handler == null || !handler.Connected)
                {
                    return;
                }

                int bytesRead = handler.EndReceive(asyncResult);

                if (bytesRead > 0)
                {
                    _gameLogger.Debug($"Message len - {bytesRead}");
                    //var packet = new Packet(stateObj.buffer.Take(bytesRead).ToArray());

                    //if (!stateObj.IdRecived)
                    //{
                    //    if (packet.MessageType != Enums.MunchkinMessageType.GetId)
                    //    {
                    //        handler.BeginReceive(stateObj.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(OnDataReceived), stateObj);
                    //        return;
                    //    }

                    //    var id = Encoding.UTF8.GetString(packet.Buffer, 4, packet.Buffer[3]); // ñ 4-ãî áàéòà èäåò ID, â 3-ì áàéòå äëèíà ID
                    //    stateObj.Id = id;
                    //    if (!ÑonfirmedConnections.ContainsKey(stateObj.Id))
                    //    {
                    //        ÑonfirmedConnections.Add(id, stateObj);
                    //    }
                    //    handler.BeginReceive(stateObj.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(OnDataReceived), stateObj);
                    //    return;
                    //}

                    //packet.SenderId = stateObj.Id;
                    //PacketSubject.OnNext(packet);

                    var pos = 0;
                    var dataLength = bytesRead + stateObj.offset;
                    if (dataLength > StateObject.BufferSize)
                    {
                        _gameLogger.Debug($"dataLength[{dataLength}] more than bufferSize[{StateObject.BufferSize}]");
                        dataLength = StateObject.BufferSize;
                    }

                    while (pos < dataLength)
                    {
                        var remaningBytes = dataLength - pos;
                        if (remaningBytes < 2)
                        {
                            stateObj.offset = 1;
                            handler.BeginReceive(stateObj.buffer, stateObj.offset, StateObject.BufferSize - stateObj.offset, 0, new AsyncCallback(OnDataReceived), stateObj);
                            return;
                        }

                        var len = BitConverter.ToInt16(stateObj.buffer, pos);
                        if (len < 5)
                        {
                            throw new Exception("Wrong message format.");
                        }

                        if (len > remaningBytes)
                        {
                            stateObj.offset = remaningBytes;
                            handler.BeginReceive(stateObj.buffer, stateObj.offset, StateObject.BufferSize - stateObj.offset, 0, new AsyncCallback(OnDataReceived), stateObj);
                            return;
                        }

                        var messageBytes = stateObj.buffer.Skip(pos).Take(len).ToArray();
                        if (messageBytes[messageBytes.Length - 1] == 4 && messageBytes[messageBytes.Length - 2] == 10)
                        {
                            var packet = new Packet(messageBytes);
                            packet.SenderId = stateObj.Id;
                            PacketSubject?.OnNext(new TcpEvent { Type = TcpEventType.ReceiveData, Data = packet } );
                        }
                        pos = pos + len;
                    }

                    stateObj.offset = 0;
                    handler.BeginReceive(stateObj.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(OnDataReceived), stateObj);
                }
                else
                {
                    handler.Close();
                    if (stateObj.IdRecived && ÑonfirmedConnections.ContainsKey(stateObj.Id))
                    {
                        ÑonfirmedConnections.Remove(stateObj.Id);
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                _gameLogger.Error("OnDataReceived: Socket has been closed");
                var stateObj = (StateObject)asyncResult.AsyncState;
                var handler = stateObj.workSocket;
            }
            catch (SocketException se)
            {
                _gameLogger.Error($"OnDataReceived: {se.Message}");
                PacketSubject?.OnNext(new TcpEvent { Type = TcpEventType.ClientDisconnect });
            }
            catch (JsonReaderException jre)
            {
                _gameLogger.Error($"OnDataReceived: {jre.Message}");
            }
            catch (Exception e)
            {
                var stateObj = (StateObject)asyncResult.AsyncState;
                var handler = stateObj.workSocket;
                if (handler != null && handler.Connected)
                {
                    stateObj.offset = 0;
                    handler.BeginReceive(stateObj.buffer, stateObj.offset, StateObject.BufferSize, 0, new AsyncCallback(OnDataReceived), stateObj);
                }
                _gameLogger.Error($"OnDataReceived unexpected: {e.Message}");
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

                //if (!_mainTcpSocket.Connected)
                //{
                //    return Result.Fail<int>("Server soket is not connected.");
                //}

                if (!ÑonfirmedConnections.ContainsKey(id))
                {
                    return Result.Fail<int>($"Soket with id - [{id}] not found.");
                }

                var remoteStateObj = ÑonfirmedConnections[id];

                if (remoteStateObj.workSocket == null || !remoteStateObj.workSocket.Connected)
                {
                    return Result.Fail<int>("Remote soket is not connected.");
                }

                var bytesSent = remoteStateObj.workSocket.Send(message);
                return Result.Ok(bytesSent);
            }
            catch (Exception e)
            {
                _gameLogger.Error($"SendMessage unexpected: {e.Message}");
                return Result.Fail<int>($"Unexpected: {e.Message}");
            }
        }

        public void StartUdpServer()
        {
            try
            {
                StopUdpServer();

                _udpSubject = new Subject<Socket>();

                _mainUdpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _mainUdpSocket.EnableBroadcast = true;
                _mainUdpSocket.Bind(new IPEndPoint(IPAddress.Any, 0));

                //_udpSubject
                //    .AsObservable()
                //    .Throttle(TimeSpan.FromSeconds(1))
                //    .Finally(() =>
                //    {
                //        _gameLogger.Debug("_udpSubject closed.");
                //    })
                //    .Subscribe(socket =>
                //    {
                //        socket.BeginSendTo(_broadcastMessage, 0, _broadcastMessage.Length, SocketFlags.None, udpClientEP, new AsyncCallback(SendCallback), socket);
                //    });

                //if (_udpSubject != null && !_udpSubject.IsDisposed)
                //{
                //    _udpSubject.OnNext(_mainUdpSocket);
                //}
            }
            catch (Exception e)
            {
                _gameLogger.Error($"StartBroadcast unexpected: {e.Message}");
            }
        }

        public void BroadcastMessage(byte[] message)
        {
            _mainUdpSocket.BeginSendTo(message, 0, message.Length, SocketFlags.None, udpClientEP, new AsyncCallback(SendCallback), _mainUdpSocket);
        }

        private void SendCallback(IAsyncResult asyncResult)
        {
            //if (_udpSubject != null && !_udpSubject.IsDisposed)
            //{
            //    _udpSubject.OnNext((Socket)asyncResult.AsyncState);
            //}
        }

        public void StopUdpServer()
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
                _gameLogger.Error($"OnDataReceived unexpected: {e.Message}");
                _mainUdpSocket?.Close();
            }
        }
    }
}
