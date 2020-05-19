using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using TcpMobile.Game.Models;
using TcpMobile.Tcp.Enums;
using TcpMobile.Tcp.Models;

namespace TcpMobile.Tcp
{
    public class MobileTcpServer
    {

        public delegate void ServerStartCallback();
        public delegate void ClientConnectCallback(string remoteIp);
        public delegate void ClientDisconnectCallback();
        public delegate void ReceiveDataCallback(string data);

        public ServerStartCallback onServerStart { get; set; }
        public ClientConnectCallback onClientConnect { get; set; }
        public ClientDisconnectCallback onClientDisconnect { get; set; }
        public ReceiveDataCallback onReceiveData { get; set; }

        private Socket _mainSocket;
        private readonly BinaryFormatter _binaryFormatter = new BinaryFormatter();
        public bool IsListening
        {
            get
            {
                if (_mainSocket == null)
                    return false;
                else
                    return _mainSocket.IsBound;
            }
        }
        public void Start(int port = 9999)
        {
            try
            {
                Stop();

                _mainSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _mainSocket.Bind(new IPEndPoint(IPAddress.Any, port));
                _mainSocket.Listen(100);
                _mainSocket.BeginAccept(new AsyncCallback(OnReceiveConnection), _mainSocket);

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
                _mainSocket.Close();
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
                    object message = null;
                    using (MemoryStream ms = new MemoryStream(stateObj.buffer))
                    {
                        TcpMessageType type = (TcpMessageType)ms.ReadByte();
                        switch (type) {
                            case TcpMessageType.Connect:
                                message = (string)_binaryFormatter.Deserialize(ms);
                                break;
                            case TcpMessageType.Disconect:
                                message = (string)_binaryFormatter.Deserialize(ms);
                                break;
                            case TcpMessageType.Ping:
                                message = (PlayerInfo)_binaryFormatter.Deserialize(ms);
                                break;
                        }

                        onReceiveData?.Invoke($"{message.ToString()} - [{bytesRead}]bytes");
                    }
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
            }
            catch (JsonReaderException jre)
            {
                Console.WriteLine($"OnDataReceived: {jre.Message}");
            }
        }
    }
}
