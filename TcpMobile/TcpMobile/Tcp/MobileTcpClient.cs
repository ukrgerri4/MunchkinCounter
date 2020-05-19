using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TcpMobile.Tcp.Models;

namespace IKTcpClient
{
    public class MobileTcpClient
    {
        private Socket _socket;

        public delegate void ReceiveDataCallback(string data);
        public ReceiveDataCallback onReceiveData { get; set; }

        public void Connect(IPAddress address, int port = 9999)
        {
            try
            {
                Disconnect();

                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.Connect(new IPEndPoint(address, port));

                if (_socket.Connected)
                {
                    var stateObj = new StateObject(_socket);
                    _socket.BeginReceive(stateObj.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(OnDataReceived), stateObj);
                }
            }

            catch (SocketException se)
            {
                Console.WriteLine(se.Message);
            }
        }

        public void Disconnect()
        {
            if (_socket != null)
                _socket.Close();
        }

        public void SendMessage(byte[] message)
        {
            if (_socket != null)
                if (_socket.Connected)
                    _socket.Send(message);
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
                    onReceiveData?.Invoke(Encoding.Unicode.GetString(stateObj.buffer, 0, bytesRead));
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
        }
    }
}
