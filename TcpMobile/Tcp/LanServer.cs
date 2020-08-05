using Core.Helpers;
using Core.Models;
using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using Infrastracture.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using TcpMobile.Tcp.Models;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace TcpMobile.Tcp
{
    public class LanServer: ILanServer
    {
        private IGameLogger _gameLogger => DependencyService.Get<IGameLogger>();

        public Dictionary<string, StateObject> ConfirmedConnections = new Dictionary<string, StateObject>();

        #region TCP
        private Socket _mainTcpSocket;
        public Subject<TcpEvent> TcpServerEventSubject { get; set; } = new Subject<TcpEvent>();
        private IDisposable _connectionChecker;
        #endregion

        #region UDP
        private Socket _mainUdpSocket;
        private IPEndPoint udpClientEP = new IPEndPoint(IPAddress.Broadcast, 42424);
        #endregion

        private bool IsConnected(Socket socket)
        {
            if (!socket.Poll(100, SelectMode.SelectRead) || socket.Available != 0)
                return true;
            return false;
        }

        public void StartTcpServer(int port = 42420)
        {
            try
            {
                StopTcpServer();

                _mainTcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _mainTcpSocket.Bind(new IPEndPoint(IPAddress.Any, port));
                _mainTcpSocket.Listen(100);
                _mainTcpSocket.LingerState = new LingerOption(true, 1);

                _gameLogger.Debug($"Start server: [{_mainTcpSocket.LocalEndPoint}]");

                _mainTcpSocket.BeginAccept(new AsyncCallback(OnReceiveConnection), _mainTcpSocket);
                
                StartConnectionsCheck();

                TcpServerEventSubject?.OnNext(new TcpEvent { Type = TcpEventType.ServerStarted });
            }
            catch (ObjectDisposedException)
            {
                _gameLogger.Error("Server StartTcpServer: socket has been closed");
            }
            catch (SocketException se)
            {
                _gameLogger.Error($"Server StartTcpServer, socket: {se.ErrorCode}, {se.SocketErrorCode}");
            }
            catch (Exception e)
            {
                _gameLogger.Error($"Server StartTcpServer, unexpected: {e.Message}");
            }
        }

        public void StopTcpServer()
        {
            try
            {
                _connectionChecker?.Dispose();

                ConfirmedConnections.ToArray()
                    .Select(c => c.Value)
                    .ForEach(stateObj => stateObj.workSocket?.Close());

                ConfirmedConnections.Clear();

                _mainTcpSocket?.Close();
            }
            catch (ObjectDisposedException)
            {
                _gameLogger.Error("Server StopTcpServer: socket has been closed");
            }
            catch (SocketException se)
            {
                _gameLogger.Error($"Server StopTcpServer, socket: {se.ErrorCode}, {se.SocketErrorCode}");
            }
            catch (Exception e)
            {
                _gameLogger.Error($"Server StopTcpServer, unexpected: {e.Message}");
            }
        }

        private void OnReceiveConnection(IAsyncResult asyncResult)
        {
            try
            {
                var listener = (Socket)asyncResult.AsyncState;
                var handler = listener.EndAccept(asyncResult);
                var stateObj = new StateObject(handler);

                TcpServerEventSubject?.OnNext(new TcpEvent { Type = TcpEventType.ClientConnected });

                ConfirmedConnections.Add(stateObj.Id, stateObj);
                handler.BeginReceive(stateObj.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(OnDataReceived), stateObj);

                listener.BeginAccept(new AsyncCallback(OnReceiveConnection), listener);
            }
            catch (ObjectDisposedException)
            {
                _gameLogger.Debug("Server OnClientConnection: socket has been closed");
            }
            catch (SocketException se)
            {
                _gameLogger.Debug($"Server OnClientConnection, socket: {se.Message}");
                TcpServerEventSubject?.OnNext(new TcpEvent { Type = TcpEventType.ClientDisconnect });
            }
            catch (Exception e)
            {
                _gameLogger.Error($"Server OnClientConnection, unexpected: {e.Message}");
            }
        }

        private void OnDataReceived(IAsyncResult asyncResult)
        {
            var stateObj = (StateObject)asyncResult.AsyncState;
            try
            {
                var handler = stateObj.workSocket;

                if (handler == null || !handler.Connected)
                {
                    return;
                }

                int bytesRead = handler.EndReceive(asyncResult);

                if (bytesRead > 0)
                {
                    _gameLogger.Debug($"Message len - {bytesRead}");

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
                            TcpServerEventSubject?.OnNext(new TcpEvent { Type = TcpEventType.ReceiveData, Data = packet } );
                        }
                        pos = pos + len;
                    }

                    stateObj.offset = 0;
                    handler.BeginReceive(stateObj.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(OnDataReceived), stateObj);
                }
                else
                {
                    handler.Close();
                    OnSocketErrorHandler(stateObj);
                }
            }
            catch (ObjectDisposedException)
            {
                _gameLogger.Error("Server OnDataReceived: socket has been closed");
                OnSocketErrorHandler(stateObj);
            }
            catch (SocketException se)
            {
                _gameLogger.Error($"Server OnDataReceived, socket: {se.ErrorCode}, {se.Message}");
                OnSocketErrorHandler(stateObj);
            }
            catch (Exception e)
            {
                _gameLogger.Error($"Server OnDataReceived unexpected: {e.Message}");
                OnSocketErrorHandler(stateObj);
            }
        }

        private void OnSocketErrorHandler(StateObject stateObj)
        {
            if (ConfirmedConnections.ContainsKey(stateObj.Id))
            {
                ConfirmedConnections.Remove(stateObj.Id);
            }
            TcpServerEventSubject?.OnNext(new TcpEvent { Type = TcpEventType.ClientDisconnect, Data = stateObj.Id });
        }

        public Result BeginSendMessage(string id, byte[] message)
        {
            try
            {
                if (_mainTcpSocket == null)
                {
                    return Result.Fail("Server BeginSendMessage: server soket is null.");
                }

                if (!ConfirmedConnections.ContainsKey(id))
                {
                    return Result.Fail($"Server BeginSendMessage: soket with id - [{id}] not found.");
                }

                var remoteStateObj = ConfirmedConnections[id];

                if (remoteStateObj.workSocket == null || !remoteStateObj.workSocket.Connected)
                {
                    return Result.Fail("Server BeginSendMessage: remote soket is not connected.");
                }

                remoteStateObj.workSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(TcpSendCallback), remoteStateObj);
                return Result.Ok();
            }
            catch (ObjectDisposedException)
            {
                var errorMessage = $"Server BeginSendMessage: socket disposed";
                return Result.Fail(errorMessage);
            }
            catch (SocketException se)
            {
                var errorMessage = $"Server BeginSendMessage, socket: {se.ErrorCode}, {se.SocketErrorCode}";
                return Result.Fail(errorMessage);
            }
            catch (Exception e)
            {
                var errorMessage = $"Server BeginSendMessage, unexpected {e.Message}";
                return Result.Fail(errorMessage);
            }
        }

        private void TcpSendCallback(IAsyncResult ar)
        {
            StateObject remoteStateObj = (StateObject)ar.AsyncState;
            try
            {
                if (remoteStateObj?.workSocket == null) { return; }
                int bytesSent = remoteStateObj.workSocket.EndSend(ar);
            }
            catch (ObjectDisposedException)
            {
                OnSocketErrorHandler(remoteStateObj);
                _gameLogger.Error("Server SendCallback: socket has been closed");
            }
            catch (SocketException se)
            {
                OnSocketErrorHandler(remoteStateObj);
                _gameLogger.Error($"Server SendCallback, socket: [{se.SocketErrorCode}] - {se.Message}");
            }
            catch (Exception e)
            {
                OnSocketErrorHandler(remoteStateObj);
                _gameLogger.Error($"Server SendCallback, UNEXPECTED: {e.Message}");
            }
        }

        private void StartConnectionsCheck()
        {
            _connectionChecker = Observable.Interval(TimeSpan.FromSeconds(1))
                .Subscribe(
                    _ =>
                    {
                        foreach(var connection in ConfirmedConnections.ToArray())
                        {
                            var stateObj = connection.Value;
                            try
                            {
                                if (stateObj.workSocket != null && IsConnected(stateObj.workSocket)) { continue;  }

                                stateObj?.workSocket?.Close();
                                OnSocketErrorHandler(stateObj);
                                _gameLogger.Debug($"Server StartConnectionsCheck: disconnected and removed");
                            }
                            catch(Exception e)
                            {
                                OnSocketErrorHandler(stateObj);
                                _gameLogger.Error($"Server StartConnectionsCheck: {e.Message}");
                            }
                        }
                    },
                    error => _gameLogger.Error($"Server StartConnectionsCheck: {error.Message}")
                );
        }

        public void StartUdpServer()
        {
            try
            {
                StopUdpServer();

                _mainUdpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _mainUdpSocket.EnableBroadcast = true;

                var ip = DnsHelper.GetLocalIp();
                _mainUdpSocket.Bind(new IPEndPoint(ip, 0));
            }
            catch (Exception e)
            {
                _gameLogger.Error($"Server StartUdpServer, error: {e.Message}");
            }
        }

        public Result BroadcastMessage(byte[] message)
        {
            try
            {
                _mainUdpSocket?.BeginSendTo(message, 0, message.Length, SocketFlags.None, udpClientEP, new AsyncCallback(UdpSendCallback), _mainUdpSocket);
                return Result.Ok();
            }
            catch (ObjectDisposedException)
            {
                return Result.Fail("Server BroadcastMessage: socket has been closed");
            }
            catch (SocketException se)
            {
                return Result.Fail($"Server BroadcastMessage, socket: [{se.ErrorCode}], [{se.SocketErrorCode}] - {se.Message}");
            }
            catch (Exception e)
            {
                return Result.Fail($"Server BroadcastMessage, unexpected: {e.Message}");
            }
        }

        private void UdpSendCallback(IAsyncResult asyncResult)
        {
            
        }

        public void StopUdpServer()
        {
            try
            {
               _mainUdpSocket?.Close();
            }
            catch (Exception e)
            {
                _gameLogger.Error($"Server StopUdpServer, error: {e.Message}");
            }
        }
    }
}