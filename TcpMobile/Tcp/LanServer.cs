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

            catch (SocketException se)
            {
                _gameLogger.Debug(se.Message);
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
            catch (Exception e)
            {
                _gameLogger.Error($"SERVER TCP SOCKET close error: {e.Message}");
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
                _gameLogger.Debug("OnClientConnection: Socket has been closed");
            }
            catch (SocketException se)
            {
                _gameLogger.Debug($"OnClientConnection: {se.Message}");
                TcpServerEventSubject?.OnNext(new TcpEvent { Type = TcpEventType.ClientDisconnect });
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
                _gameLogger.Error("Server OnDataReceived disposed error: Socket has been closed");
                OnSocketErrorHandler(stateObj);
            }
            catch (SocketException se)
            {
                _gameLogger.Error($"Server OnDataReceived socket error: {se.Message}");
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

        public Result<int> SendMessage(string id, byte[] message)
        {
            try
            {
                if (_mainTcpSocket == null)
                {
                    return Result.Fail<int>("Server soket is null.");
                }

                if (!ConfirmedConnections.ContainsKey(id))
                {
                    return Result.Fail<int>($"Soket with id - [{id}] not found.");
                }

                var remoteStateObj = ConfirmedConnections[id];

                if (remoteStateObj.workSocket == null || !remoteStateObj.workSocket.Connected)
                {
                    return Result.Fail<int>("Remote soket is not connected.");
                }

                var bytesSent = remoteStateObj.workSocket.Send(message);
                return Result.Ok(bytesSent);
            }
            catch (Exception e)
            {
                _gameLogger.Error($"Server sendMessage unexpected: {e.Message}");
                return Result.Fail<int>($"Unexpected: {e.Message}");
            }
        }

        public Result BeginSendMessage(string id, byte[] message)
        {
            try
            {
                if (_mainTcpSocket == null)
                {
                    return Result.Fail("Server soket is null.");
                }

                if (!ConfirmedConnections.ContainsKey(id))
                {
                    return Result.Fail($"Soket with id - [{id}] not found.");
                }

                var remoteStateObj = ConfirmedConnections[id];

                if (remoteStateObj.workSocket == null || !remoteStateObj.workSocket.Connected)
                {
                    return Result.Fail("Remote soket is not connected.");
                }

                remoteStateObj.workSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(TcpSendCallback), remoteStateObj);
                return Result.Ok();
            }
            catch (Exception e)
            {
                _gameLogger.Error($"Server sendMessage unexpected: {e.Message}");
                return Result.Fail($"Unexpected: {e.Message}");
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
                _gameLogger.Error("Server SendCallback: Socket has been closed");
            }
            catch (SocketException se)
            {
                OnSocketErrorHandler(remoteStateObj);
                _gameLogger.Error($"Server SendCallback: [{se.SocketErrorCode}] - {se.Message}");
            }
            catch (Exception e)
            {
                OnSocketErrorHandler(remoteStateObj);
                _gameLogger.Error($"Server SendCallback UNEXPECTED: {e.Message}");
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
                                _gameLogger.Debug($"Server Connection check: disconnected and removed");
                            }
                            catch(Exception e)
                            {
                                OnSocketErrorHandler(stateObj);
                                _gameLogger.Error($"Server Connection check error: {e.Message}");
                            }
                        }
                    },
                    error => _gameLogger.Error($"Server Connection check subscribe error: {error.Message}")
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
                _gameLogger.Error($"Server start UDP server error: {e.Message}");
            }
        }

        public void BroadcastMessage(byte[] message)
        {
            _mainUdpSocket.BeginSendTo(message, 0, message.Length, SocketFlags.None, udpClientEP, new AsyncCallback(UdpSendCallback), _mainUdpSocket);
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
                _gameLogger.Error($"Server stop UDP server error: {e.Message}");
            }
        }
    }
}