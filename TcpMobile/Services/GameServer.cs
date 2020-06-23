using Core.Models;
using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using Infrastracture.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using TcpMobile.Game.Models;
using TcpMobile.Tcp.Enums;
using TcpMobile.Tcp.Models;

namespace TcpMobile.Services
{
    public class GameServer : IGameServer
    {
        private readonly IGameLogger _gameLogger;
        private readonly IConfiguration _configuration;
        private readonly ILanServer _lanServer;

        private IDisposable _hostBroadcaster;
        private IDisposable _connectedPlayersBroadcaster;

        private Subject<Unit> _updatePlayersSubject = new Subject<Unit>();
        private Subject<Unit> _destroy = new Subject<Unit>();

        private double HOST_BROADCAST_PERIOD_MS = 1000;
        private double CONNECTED_PLAYERS_BROADCAST_PERIOD_MS = 500;

        public MunchkinHost Host { get; set; }
        public ConcurrentDictionary<string, PlayerInfo> ConnectedPlayers { get; set; }

        public GameServer(
            IGameLogger gameLogger,
            IConfiguration configuration,
            ILanServer lanServer
         )
        {
            _gameLogger = gameLogger;
            _configuration = configuration;
            _lanServer = lanServer;

            Host = new MunchkinHost
            {
                Id = _configuration["DeviceId"],
                Name = "Game_1"
            };

            ConnectedPlayers = new ConcurrentDictionary<string, PlayerInfo>();
        }

        public Result Start()
        {
            try
            {
                _lanServer.StartTcpServer();
                _lanServer.StartUdpServer();

                StartBroadcastHostData();
                StartBroadcastConnectedPlayersData();
                StartListeningNewPlayersConnections();
                StartListeningPlayersDisconnections();

                return Result.Ok();
            }
            catch (Exception e)
            {
                var errorMessage = $"Start game server error: {e.Message}";
                _gameLogger.Error(errorMessage);
                return Result.Fail(errorMessage);
            }
        }

        public Result Stop()
        {
            try
            {
                StopConnectedPlayersBroadcast();

                _lanServer.StopUdpServer();
                _lanServer.StopTcpServer();

                _destroy.OnNext(Unit.Default);

                Host.Fullness = 0;
                ConnectedPlayers.Clear();

                return Result.Ok();
            }
            catch (Exception e)
            {
                var errorMessage = $"Stop game server error: {e.Message}";
                _gameLogger.Error(errorMessage);
                return Result.Fail(errorMessage);
            }
        }

        public Result StopBroadcast()
        {
            try
            {
                _hostBroadcaster?.Dispose();
                _lanServer.StopUdpServer();
                return Result.Ok();
            }
            catch(Exception e)
            {
                return Result.Fail($"Stop broadcasting error: {e.Message}");
            }
        }

        public void StopConnectedPlayersBroadcast()
        {
            _connectedPlayersBroadcaster?.Dispose();
        }

        private void StartBroadcastHostData()
        {
            _hostBroadcaster = Observable.Interval(TimeSpan.FromMilliseconds(HOST_BROADCAST_PERIOD_MS))
                .TakeUntil(_destroy)
                .Finally(() => _gameLogger.Debug("Host data broadcast stoped."))
                //.Where(_ => string.IsNullOrWhiteSpace(Host.Id) && string.IsNullOrWhiteSpace(Host.Name))
                .Select(data =>
                {
                    using (MemoryStream memoryStream = new MemoryStream())
                    {
                        memoryStream.Write(BitConverter.GetBytes((ushort)0), 0, 2);
                        memoryStream.WriteByte((byte)MunchkinMessageType.HostFound);

                        var byteId = Encoding.UTF8.GetBytes(Host.Id ?? string.Empty);
                        memoryStream.WriteByte((byte)byteId.Length);
                        memoryStream.Write(byteId, 0, byteId.Length);

                        var byteName = Encoding.UTF8.GetBytes(Host.Name ?? string.Empty);
                        memoryStream.WriteByte((byte)byteName.Length);
                        memoryStream.Write(byteName, 0, byteName.Length);

                        memoryStream.WriteByte(Host.Capacity);
                        memoryStream.WriteByte(Host.Fullness);

                        memoryStream.Seek(0, SeekOrigin.Begin);
                        memoryStream.Write(BitConverter.GetBytes((ushort)memoryStream.Length), 0, 2);
                        memoryStream.Seek(0, SeekOrigin.End);

                        return memoryStream.ToArray();
                    }
                })
                .Do(_ => _gameLogger.Debug("Sart send host data"))
                .Subscribe(
                    message =>
                    {
                        _lanServer.BroadcastMessage(message);
                    },
                    error =>
                    {
                        _gameLogger.Error($"Error during broadcast host: {error.Message}");
                    }
                );
        }

        private void StartListeningNewPlayersConnections()
        {
            _lanServer.TcpEventSubject.AsObservable()
                .TakeUntil(_destroy)
                .Where(tcpEvent => tcpEvent.Type == TcpEventType.ReceiveData)
                .Where(tcpEvent => tcpEvent.Data != null)
                .Where(tcpEvent => ((Packet)tcpEvent.Data).MessageType == MunchkinMessageType.InitInfo ||
                    ((Packet)tcpEvent.Data).MessageType == MunchkinMessageType.UpdatePlayerState ||
                    ((Packet)tcpEvent.Data).MessageType == MunchkinMessageType.UpdatePlayerName)
                .Do(tcpEvent => _gameLogger.Debug($"Recieved message {((Packet)tcpEvent.Data).MessageType}"))
                .Subscribe(
                    tcpEvent =>
                    {
                        var packet = (Packet)tcpEvent.Data;
                        var position = 3;

                        switch (packet.MessageType)
                        {
                            case MunchkinMessageType.InitInfo:
                                var playerInfo = new PlayerInfo();

                                playerInfo.Id = Encoding.UTF8.GetString(packet.Buffer, position + 1, packet.Buffer[position]);
                                position += packet.Buffer[position];
                                position++;

                                playerInfo.Name = Encoding.UTF8.GetString(packet.Buffer, position + 1, packet.Buffer[position]);
                                position += packet.Buffer[position];
                                position++;

                                playerInfo.Level = packet.Buffer[position++];
                                playerInfo.Modifiers = packet.Buffer[position++];

                                if (packet.SenderId != playerInfo.Id)
                                {
                                    // log some warning
                                }

                                var playerAdded = ConnectedPlayers.TryAdd(packet.SenderId, playerInfo);

                                if (!playerAdded)
                                {
                                    ConnectedPlayers[packet.SenderId].Name = playerInfo.Name;
                                    ConnectedPlayers[packet.SenderId].Level = playerInfo.Level;
                                    ConnectedPlayers[packet.SenderId].Modifiers = playerInfo.Modifiers;
                                }
                                else
                                {
                                    Host.Fullness++;
                                }

                                break;
                            case MunchkinMessageType.UpdatePlayerState:
                                var level = packet.Buffer[position++];
                                var modifiers = packet.Buffer[position++];

                                if (ConnectedPlayers.ContainsKey(packet.SenderId))
                                {
                                    ConnectedPlayers[packet.SenderId].Level = level;
                                    ConnectedPlayers[packet.SenderId].Modifiers = modifiers;
                                }

                                break;
                            case MunchkinMessageType.UpdatePlayerName:
                                var name = Encoding.UTF8.GetString(packet.Buffer, position + 1, packet.Buffer[position]);

                                if (ConnectedPlayers.ContainsKey(packet.SenderId))
                                {
                                    ConnectedPlayers[packet.SenderId].Name = name;
                                }
                                break;
                        }

                        _updatePlayersSubject.OnNext(Unit.Default);
                    },
                    error =>
                    {
                        _gameLogger.Error($"Error during listening for new players: {error.Message}");
                    }
                );
        }

        private void StartListeningPlayersDisconnections()
        {
            _lanServer.TcpEventSubject.AsObservable()
                .TakeUntil(_destroy)
                .Where(tcpEvent => tcpEvent.Type == TcpEventType.ClientDisconnect)
                .Where(tcpEvent => (string)tcpEvent.Data != null)
                .Do(tcpEvent => _gameLogger.Debug($"Client disconnected handler - [{(string)tcpEvent.Data}]"))
                .Subscribe(
                    tcpEvent =>
                    {
                        var removeResult = ConnectedPlayers.TryRemove((string)tcpEvent.Data, out PlayerInfo pi);
                        if (!removeResult)
                        {
                            _gameLogger.Error($"Player connection id:[{(string)tcpEvent.Data}] not found.");
                            return;
                        }

                        Host.Fullness--;
                        _updatePlayersSubject.OnNext(Unit.Default);
                    },
                    error =>
                    {
                        _gameLogger.Error($"Error during listening for new players: {error.Message}");
                    }
                );
        }

        private void StartBroadcastConnectedPlayersData()
        {
            _connectedPlayersBroadcaster = _updatePlayersSubject.AsObservable()
                .TakeUntil(_destroy)
                .Finally(() => _gameLogger.Debug("Connected players data broadcast stoped."))
                .Throttle(TimeSpan.FromMilliseconds(CONNECTED_PLAYERS_BROADCAST_PERIOD_MS))
                .Do(_ => _gameLogger.Debug("Start send players data."))
                .Subscribe(
                    _ =>
                    {
                        var players = ConnectedPlayers.ToArray();

                        using (MemoryStream memoryStream = new MemoryStream())
                        {
                            memoryStream.Write(BitConverter.GetBytes((ushort)0), 0, 2);
                            memoryStream.WriteByte((byte)MunchkinMessageType.UpdatePlayers);
                            memoryStream.WriteByte((byte)players.Length);

                            foreach (var player in players)
                            {
                                var byteId = Encoding.UTF8.GetBytes(player.Value.Id ?? string.Empty);
                                memoryStream.WriteByte((byte)byteId.Length);
                                memoryStream.Write(byteId, 0, byteId.Length);

                                var byteName = Encoding.UTF8.GetBytes(player.Value.Name ?? string.Empty);
                                memoryStream.WriteByte((byte)byteName.Length);
                                memoryStream.Write(byteName, 0, byteName.Length);

                                memoryStream.WriteByte(player.Value.Level);
                                memoryStream.WriteByte(player.Value.Modifiers);
                            }

                            memoryStream.Seek(0, SeekOrigin.Begin);
                            memoryStream.Write(BitConverter.GetBytes((ushort)memoryStream.Length), 0, 2);
                            memoryStream.Seek(0, SeekOrigin.End);

                            var message = memoryStream.ToArray();

                            foreach (var player in players)
                            {
                                _lanServer.SendMessage(player.Key, message);
                            }
                        }
                    },
                    error =>
                    {
                        _gameLogger.Error($"Error during broadcast connected players: {error.Message}");
                    }
                );
        }
    }
}
