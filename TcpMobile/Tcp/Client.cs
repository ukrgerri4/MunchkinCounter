﻿using Core.Models;
using Infrastracture.Interfaces.GameMunchkin;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Subjects;
using System.Text;
using TcpMobile.Tcp.Models;

namespace TcpMobile.Tcp
{
    public class Client: IGameClient
    {
        public Subject<Packet> PacketSubject { get; set; } = new Subject<Packet>();

        private Socket _mainTcpSocket;
        private Socket _mainUdpSocket;
    
        private readonly IConfiguration _configuration;

        public Client(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Result Connect(IPAddress address, int port = 42420)
        {
            try
            {
                Disconnect();

                _mainTcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _mainTcpSocket.Connect(new IPEndPoint(address, port));

                if (!_mainTcpSocket.Connected) { return Result.Fail("Soket is not connected."); }
                
                var stateObj = new StateObject(_mainTcpSocket);
                stateObj.Id = _configuration["DeviceId"];
                _mainTcpSocket.BeginReceive(stateObj.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(OnDataReceived), stateObj);

                return Result.Ok();
            }

            catch (SocketException se)
            {
                Console.WriteLine(se.Message);
                return Result.Fail(se.Message);
            }
        }

        public void Disconnect()
        {
            _mainTcpSocket?.Close();
        }

        public Result<int> SendMessage(byte[] message)
        {
            if (_mainTcpSocket == null)
            {
                return Result.Fail<int>("Soket is null.");
            }

            if (!_mainTcpSocket.Connected)
            {
                return Result.Fail<int>("Soket is not connected.");
            }

            var bytesSent = _mainTcpSocket.Send(message);
            return Result.Ok(bytesSent);
        }

        private void OnDataReceived(IAsyncResult asyncResult)
        {
            try
            {
                var stateObj = (StateObject)asyncResult.AsyncState;
                var handler = stateObj.workSocket;

                if (handler == null || !handler.Connected || handler.Available == 0)
                {
                    return;
                }

                int bytesRead = handler.EndReceive(asyncResult);

                if (bytesRead > 0)
                {
                    var packet = new Packet(stateObj.buffer.Take(bytesRead).ToArray());
                    PacketSubject.OnNext(packet);
                    handler.BeginReceive(stateObj.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(OnDataReceived), stateObj);
                }
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
            catch (Exception e)
            {
                Console.WriteLine($"StartListeningBroadcast unexpected: {e.Message}");
            }
        }

        private void ReceiveBroadcastCallback(IAsyncResult asyncResult)
        {
            var stateObj = (StateObject)asyncResult.AsyncState;

            if (stateObj == null || stateObj.workSocket == null) { return; }
            
            EndPoint clientEp = new IPEndPoint(IPAddress.Any, 0);
            int bytesRead = stateObj.workSocket.EndReceiveFrom(asyncResult, ref clientEp);
            
            Console.WriteLine($"{clientEp} - [{bytesRead}]b");
            if (bytesRead == 1 && stateObj.buffer[0] == 42) {
                StopListeningBroadcast();
                Console.WriteLine($"Starting connect to - {((IPEndPoint)clientEp).Address}");
                Connect(((IPEndPoint)clientEp).Address);
                return;
            }

            stateObj.workSocket.BeginReceiveFrom(stateObj.buffer, 0, stateObj.buffer.Length, SocketFlags.None, ref clientEp, new AsyncCallback(ReceiveBroadcastCallback), stateObj);
        }

        public void StopListeningBroadcast()
        {
            try
            {
                _mainUdpSocket?.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine($"StopListeningBroadcast unexpected: {e.Message}");
            }
        }
    }
}