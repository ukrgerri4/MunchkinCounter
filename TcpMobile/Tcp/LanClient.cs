using Core.Models;
using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using Infrastracture.Models;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using TcpMobile.Tcp.Enums;
using TcpMobile.Tcp.Models;
using Xamarin.Forms;

namespace TcpMobile.Tcp
{
    public class LanClient: ILanClient
    {
        private IGameLogger _gameLogger => DependencyService.Get<IGameLogger>();
        private IDeviceInfoService _deviceInfoService => DependencyService.Get<IDeviceInfoService>();

        public Subject<TcpEvent> TcpClientEventSubject { get; set; } = new Subject<TcpEvent>();
        private IDisposable _connectionChecker;

        private Socket _mainTcpSocket;
        private Socket _mainUdpSocket;

        private byte[] DEFAULT_PING_MESSAGE = new byte[] { 5, 0, (byte)MunchkinMessageType.Ping, 10, 4 };


        private bool IsConnected(Socket socket)
        {
            if (!socket.Poll(100, SelectMode.SelectRead))
            {
                return true;
            }
            else if (socket.Available != 0)
            {
                return true;
            }
            else
            {
                try
                {
                    socket.Send(DEFAULT_PING_MESSAGE, 5, SocketFlags.None);
                    return true;
                }
                catch (SocketException e)
                {
                    if (e.NativeErrorCode.Equals(10035))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public Result Connect(IPAddress address, int port = 42420)
        {
            try
            {
                Disconnect();

                _mainTcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _mainTcpSocket.Connect(new IPEndPoint(address, port));
                _mainTcpSocket.LingerState = new LingerOption(true, 1);

                if (!_mainTcpSocket.Connected) { return Result.Fail("Client Connect: soket is not connected."); }
                
                var stateObj = new StateObject(_mainTcpSocket);
                stateObj.Id = _deviceInfoService.DeviceId;
                _mainTcpSocket.BeginReceive(stateObj.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(OnDataReceived), stateObj);

                StartConnectionCheck();

                return Result.Ok();
            }
            catch (ObjectDisposedException)
            {
                return Result.Fail($"Client Connect: socket disposed");
            }
            catch (SocketException se)
            {
                return Result.Fail($"Client Connect, socket: {se.ErrorCode}, {se.SocketErrorCode}");
            }
            catch (Exception e)
            {
                return Result.Fail($"Client Connect, unexpected {e.Message}");
            }
        }

        private void StartConnectionCheck()
        {
            _connectionChecker = Observable.Interval(TimeSpan.FromSeconds(1))
                .Subscribe(
                    _ =>
                    {
                        try
                        {
                            if (_mainTcpSocket != null && IsConnected(_mainTcpSocket)) { return; }

                            Disconnect();
                            TcpClientEventSubject.OnNext(new TcpEvent { Type = TcpEventType.StopServerConnection });
                            _gameLogger.Debug($"Server connection check: disconnected");
                        }
                        catch (Exception e)
                        {
                            try
                            {
                                Disconnect();
                                TcpClientEventSubject.OnNext(new TcpEvent { Type = TcpEventType.StopServerConnection });
                            }
                            catch { }

                            _gameLogger.Error($"Server connection check error: {e.Message}");
                        }
                    },
                    error => _gameLogger.Error($"Server connection check SUBSCRIPTION error: {error.Message}")
                );
        }

        public void Disconnect()
        {
            try
            {
                _connectionChecker?.Dispose();
                _mainTcpSocket?.Close();
            }
            catch (ObjectDisposedException)
            {
                _gameLogger.Error("Client Disconnect: socket has been closed");
            }
            catch (SocketException se)
            {
                _gameLogger.Error($"Client Disconnect, socket: {se.ErrorCode}, {se.SocketErrorCode}");
            }
            catch (Exception e)
            {
                _gameLogger.Error($"Client Disconnect, unexpected: {e.Message}");
            }
        }

        public Result BeginSendMessage(byte[] message)
        {
            try
            {
                if (_mainTcpSocket == null)
                {
                    return Result.Fail("Soket is null.");
                }

                if (!_mainTcpSocket.Connected)
                {
                    return Result.Fail("Soket is not connected.");
                }

                _mainTcpSocket.BeginSend(message, 0, message.Length, SocketFlags.None, new AsyncCallback(SendCallback), _mainTcpSocket);
                return Result.Ok();
            }
            catch (ObjectDisposedException)
            {
                return Result.Fail("Client BeginSendMessage: Socket has been closed");
            }
            catch (SocketException se)
            {
                return Result.Fail($"Client BeginSendMessage: {se.ErrorCode}, {se.SocketErrorCode}");
            }
            catch (Exception e)
            {
                return Result.Fail($"Client BeginSendMessage UNEXPECTED: {e.Message}");
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            Socket socket = (Socket)ar.AsyncState;
            try
            {
                int bytesSent = socket.EndSend(ar);
            }
            catch (ObjectDisposedException)
            {
                Disconnect();
                _gameLogger.Error("Client SendCallback: Socket has been closed");
            }
            catch (SocketException se)
            {
                Disconnect();
                _gameLogger.Error($"Client SendCallback: {se.ErrorCode}, {se.SocketErrorCode}");
            }
            catch (Exception e)
            {
                Disconnect();
                _gameLogger.Error($"Client SendCallback UNEXPECTED: {e.Message}");
            }
        }

        private void OnDataReceived(IAsyncResult ar)
        {
            try
            {
                var stateObj = (StateObject)ar.AsyncState;
                var handler = stateObj.workSocket;

                if (handler == null || !handler.Connected)
                {
                    return;
                }

                int bytesRead = handler.EndReceive(ar);

                if (bytesRead > 0)
                {
                    var packet = new Packet(stateObj.buffer.Take(bytesRead).ToArray());
                    TcpClientEventSubject?.OnNext(new TcpEvent { Type = TcpEventType.ReceiveData, Data = packet } );
                    handler.BeginReceive(stateObj.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(OnDataReceived), stateObj);
                }
            }
            catch (ObjectDisposedException)
            {
                Disconnect();
                TcpClientEventSubject.OnNext(new TcpEvent { Type = TcpEventType.StopServerConnection });
                _gameLogger.Error("Client OnDataReceived: Socket has been closed");
            }
            catch (SocketException se)
            {
                Disconnect();
                TcpClientEventSubject.OnNext(new TcpEvent { Type = TcpEventType.StopServerConnection });
                _gameLogger.Error($"Client OnDataReceived: {se.ErrorCode}, {se.SocketErrorCode}");
            }
            catch (Exception e)
            {
                Disconnect();
                TcpClientEventSubject.OnNext(new TcpEvent { Type = TcpEventType.StopServerConnection });
                _gameLogger.Error($"Client OnDataReceived UNEXPECTED: {e.Message}");
            }
        }

        public void StartListeningBroadcast(int port = 42424)
        {
            try
            {
                StopListeningBroadcast();

                _mainUdpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _mainUdpSocket.EnableBroadcast = true;
                _mainUdpSocket.Bind(new IPEndPoint(IPAddress.Any, port));

                var stateObj = new StateObject(_mainUdpSocket);

                EndPoint clientEp = new IPEndPoint(IPAddress.Any, 0);
                _mainUdpSocket?.BeginReceiveFrom(stateObj.buffer, 0, stateObj.buffer.Length, SocketFlags.None, ref clientEp, new AsyncCallback(ReceiveBroadcastCallback), stateObj);
            }
            catch (ObjectDisposedException)
            {
                _gameLogger.Error("Client StartListeningBroadcast: socket has been closed");
            }
            catch (SocketException se)
            {
                _gameLogger.Error($"Client StartListeningBroadcast, socket: {se.ErrorCode}, {se.SocketErrorCode}");
            }
            catch (Exception e)
            {
                _gameLogger.Error($"Client StartListeningBroadcast, unexpected: {e.Message}");
            }
        }

        private void ReceiveBroadcastCallback(IAsyncResult ar)
        {
            try
            {
                var stateObj = (StateObject)ar.AsyncState;

                if (stateObj == null || stateObj.workSocket == null) { return; }

                EndPoint clientEp = new IPEndPoint(IPAddress.Any, 0);
                int bytesRead = stateObj.workSocket.EndReceiveFrom(ar, ref clientEp);

                _gameLogger.Debug($"{clientEp} - [{bytesRead}]b");
                var packet = new Packet(stateObj.buffer.Take(bytesRead).ToArray());
                packet.SenderId = stateObj.Id;
                packet.SenderIpAdress = ((IPEndPoint)clientEp).Address;
                TcpClientEventSubject?.OnNext(new TcpEvent { Type = TcpEventType.ReceiveData, Data = packet });

                _gameLogger.Debug($"Client begin recieve broadcast messages");
                stateObj.workSocket.BeginReceiveFrom(stateObj.buffer, 0, stateObj.buffer.Length, SocketFlags.None, ref clientEp, new AsyncCallback(ReceiveBroadcastCallback), stateObj);
            }
            catch (ObjectDisposedException)
            {
                _gameLogger.Error($"Client ReceiveBroadcastCallback socket disposed");
            }
            catch (SocketException se)
            {
                _gameLogger.Error($"Client ReceiveBroadcastCallback socket: {se.ErrorCode}, {se.SocketErrorCode}");
            }
            catch (Exception e)
            {
                _gameLogger.Error($"Client ReceiveBroadcastCallback unexpected: {e.Message}");
            }
        }

        public void StopListeningBroadcast()
        {
            try
            {
                _mainUdpSocket?.Close();
            }
            catch (ObjectDisposedException)
            {
                _gameLogger.Error("Client StopListeningBroadcast: socket has been closed");
            }
            catch (SocketException se)
            {
                _gameLogger.Error($"Client StopListeningBroadcast, socket: {se.ErrorCode}, {se.SocketErrorCode}");
            }
            catch (Exception e)
            {
                _gameLogger.Error($"Client StopListeningBroadcast, unexpected: {e.Message}");
            }
        }
    }
}
