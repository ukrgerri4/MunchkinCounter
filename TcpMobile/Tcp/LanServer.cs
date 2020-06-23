using Core.Helpers;
using Core.Models;
using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using Infrastracture.Models;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _configuration;

        public Dictionary<string, StateObject> ConfirmedConnections = new Dictionary<string, StateObject>();

        #region TCP
        private Socket _mainTcpSocket;
        public Subject<TcpEvent> TcpEventSubject { get; set; } = new Subject<TcpEvent>();
        private IDisposable _connectionChecker;
        #endregion

        #region UDP
        private Socket _mainUdpSocket;
        private IPEndPoint udpClientEP = new IPEndPoint(IPAddress.Broadcast, 42424);
        #endregion

        public LanServer(
            IConfiguration configuration,
            IGameLogger gameLogger)
        {
            _configuration = configuration;
            _gameLogger = gameLogger;
        }

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

                _gameLogger.Debug($"Start server: [{_mainTcpSocket.LocalEndPoint}]");

                _mainTcpSocket.BeginAccept(new AsyncCallback(OnReceiveConnection), _mainTcpSocket);
                
                StartConnectionsCheck();

                TcpEventSubject?.OnNext(new TcpEvent { Type = TcpEventType.ServerStarted });
            }

            catch (SocketException se)
            {
                _gameLogger.Debug(se.Message);
            }
        }

        public void StopTcpServer()
        {
            _mainTcpSocket?.Close();
            _connectionChecker?.Dispose();
            ConfirmedConnections.Clear();
        }

        private void OnReceiveConnection(IAsyncResult asyncResult)
        {
            try
            {
                var listener = (Socket)asyncResult.AsyncState;
                var handler = listener.EndAccept(asyncResult);
                var stateObj = new StateObject(handler);

                TcpEventSubject?.OnNext(new TcpEvent { Type = TcpEventType.ClientConnected });

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
                TcpEventSubject?.OnNext(new TcpEvent { Type = TcpEventType.ClientDisconnect });
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
                            TcpEventSubject?.OnNext(new TcpEvent { Type = TcpEventType.ReceiveData, Data = packet } );
                        }
                        pos = pos + len;
                    }

                    stateObj.offset = 0;
                    handler.BeginReceive(stateObj.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(OnDataReceived), stateObj);
                }
                else
                {
                    handler.Close();
                    OnDataReceivedErrorHandler(stateObj);
                }
            }
            catch (ObjectDisposedException)
            {
                _gameLogger.Error("OnDataReceived disposed error: Socket has been closed");
                OnDataReceivedErrorHandler(stateObj);
            }
            catch (SocketException se)
            {
                _gameLogger.Error($"OnDataReceived socket error: {se.Message}");
                OnDataReceivedErrorHandler(stateObj);
            }
            catch (Exception e)
            {
                _gameLogger.Error($"OnDataReceived unexpected: {e.Message}");
                OnDataReceivedErrorHandler(stateObj);
            }
        }

        private void OnDataReceivedErrorHandler(StateObject stateObj)
        {
            if (ConfirmedConnections.ContainsKey(stateObj.Id))
            {
                ConfirmedConnections.Remove(stateObj.Id);
            }
            TcpEventSubject?.OnNext(new TcpEvent { Type = TcpEventType.ClientDisconnect, Data = stateObj.Id });
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
                _gameLogger.Error($"SendMessage unexpected: {e.Message}");
                return Result.Fail<int>($"Unexpected: {e.Message}");
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
                                if (stateObj.workSocket != null && IsConnected(stateObj.workSocket)) { return;  }

                                stateObj?.workSocket?.Close();
                                OnDataReceivedErrorHandler(stateObj);
                                _gameLogger.Debug($"Connection check: disconnected and removed");
                            }
                            catch(Exception e)
                            {
                                OnDataReceivedErrorHandler(stateObj);
                                _gameLogger.Error($"Connection check error: {e.Message}");
                            }
                        }
                    },
                    error => _gameLogger.Error($"Connection check subscribe error: {error.Message}")
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
                _gameLogger.Error($"Start UDP server error: {e.Message}");
            }
        }

        public void BroadcastMessage(byte[] message)
        {
            _mainUdpSocket.BeginSendTo(message, 0, message.Length, SocketFlags.None, udpClientEP, new AsyncCallback(SendCallback), _mainUdpSocket);
        }

        private void SendCallback(IAsyncResult asyncResult)
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
                _gameLogger.Error($"Stop UDP server error: {e.Message}");
            }
        }
    }
}