using Core.Models;
using GameMunchkin.Models;
using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using Infrastracture.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using TcpMobile.Game.Models;
using TcpMobile.Tcp.Enums;
using TcpMobile.Tcp.Models;
using Xamarin.Forms;

namespace TcpMobile.Services
{
    public class GameClient : IGameClient
    {
        private readonly IGameLogger _gameLogger;
        private readonly IConfiguration _configuration;
        private readonly ILanClient _lanClient;

        public ObservableCollection<MunchkinHost> Hosts { get; set; }
        public Player MyPlayer { get; set; }
        public List<Player> Players { get; set; }

        
        private IDisposable _hostsSearchSubscribe;

        private Subject<Unit> _destroy = new Subject<Unit>();

        public GameClient(
            IGameLogger gameLogger,
            IConfiguration configuration,
            ILanClient lanClient
         )
        {
            _gameLogger = gameLogger;
            _configuration = configuration;
            _lanClient = lanClient;

            Hosts = new ObservableCollection<MunchkinHost>();

            MyPlayer = new Player
            {
                Id = _configuration["DeviceId"],
                Name = "Player_1"
            };
            Players = new List<Player>();
        }

        public Result Connect(IPAddress ip)
        {
            return _lanClient.Connect(ip);
        }

        public Result Stop()
        {
            try
            {
                _hostsSearchSubscribe?.Dispose();
                _destroy.OnNext(Unit.Default);
                _lanClient.StopListeningBroadcast();
                _lanClient.Disconnect();

                Hosts.Clear();
                Players.Clear();
                MyPlayer.Level = 1;
                MyPlayer.Modifiers = 0;

                MessagingCenter.Send<IGameClient>(this, "PlayersUpdated");

                return Result.Ok();
            }
            catch (Exception e)
            {
                return Result.Fail($"Client stop error: {e.Message}");
            }
        }

        public void ConnectSelf()
        {
            var localIPs = Dns.GetHostAddresses(Dns.GetHostName());

            _gameLogger.Debug($"Self connections => {string.Join(",", localIPs.Select(ip => ip.ToString()))}");

            var localIp = localIPs.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork && ip.GetAddressBytes()[0] == 192);

            if (localIp == null) { throw new ArgumentNullException(nameof(localIp)); }

            _lanClient.Connect(localIp);
        }

        public Result SendPlayerInfo()
        {
            //if (_lanClient.NotConnected)... return

            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Write(BitConverter.GetBytes((ushort)0), 0, 2);
                memoryStream.WriteByte((byte)MunchkinMessageType.InitInfo);

                var byteId = Encoding.UTF8.GetBytes(MyPlayer.Id ?? string.Empty);
                memoryStream.WriteByte((byte)byteId.Length);
                memoryStream.Write(byteId, 0, byteId.Length);

                var byteName = Encoding.UTF8.GetBytes(MyPlayer.Name ?? string.Empty);
                memoryStream.WriteByte((byte)byteName.Length);
                memoryStream.Write(byteName, 0, byteName.Length);

                memoryStream.WriteByte(MyPlayer.Sex);
                memoryStream.WriteByte(MyPlayer.Level);
                memoryStream.WriteByte(MyPlayer.Modifiers);

                memoryStream.WriteByte(10);
                memoryStream.WriteByte(4);

                memoryStream.Seek(0, SeekOrigin.Begin);
                memoryStream.Write(BitConverter.GetBytes((ushort)memoryStream.Length), 0, 2);
                memoryStream.Seek(0, SeekOrigin.End);
                
                var initMessageResult = _lanClient.BeginSendMessage(memoryStream.ToArray());
                
                if (initMessageResult.IsFail)
                {
                    return Result.Fail("Init message during connect to self fail.");
                }

                return Result.Ok();
            }
        }

        public Result SendUpdatedPlayerState()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Write(BitConverter.GetBytes((ushort)0), 0, 2);
                memoryStream.WriteByte((byte)MunchkinMessageType.UpdatePlayerState);
                memoryStream.WriteByte(MyPlayer.Sex);
                memoryStream.WriteByte(MyPlayer.Level);
                memoryStream.WriteByte(MyPlayer.Modifiers);

                memoryStream.WriteByte(10);
                memoryStream.WriteByte(4);

                memoryStream.Seek(0, SeekOrigin.Begin);
                memoryStream.Write(BitConverter.GetBytes((ushort)memoryStream.Length), 0, 2);
                memoryStream.Seek(0, SeekOrigin.End);
                
                var initMessageResult = _lanClient.BeginSendMessage(memoryStream.ToArray());

                if (initMessageResult.IsFail)
                {
                    return Result.Fail("Update state message fail.");
                }

                return Result.Ok();
            }
        }

        public Result SendUpdatedPlayerName()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                memoryStream.Write(BitConverter.GetBytes((ushort)0), 0, 2);
                memoryStream.WriteByte((byte)MunchkinMessageType.UpdatePlayerName);

                var byteName = Encoding.UTF8.GetBytes(MyPlayer.Name ?? string.Empty);
                memoryStream.WriteByte((byte)byteName.Length);
                memoryStream.Write(byteName, 0, byteName.Length);

                memoryStream.WriteByte(10);
                memoryStream.WriteByte(4);

                memoryStream.Seek(0, SeekOrigin.Begin);
                memoryStream.Write(BitConverter.GetBytes((ushort)memoryStream.Length), 0, 2);
                memoryStream.Seek(0, SeekOrigin.End);

                var initMessageResult = _lanClient.BeginSendMessage(memoryStream.ToArray());

                if (initMessageResult.IsFail)
                {
                    return Result.Fail("Update name message fail.");
                }

                return Result.Ok();
            }
        }

        public void StartUpdatePlayers()
        {
            _lanClient.PacketSubject.AsObservable()
                .TakeUntil(_destroy)
                .Where(tcpEvent => tcpEvent.Type == TcpEventType.ReceiveData)
                .Where(tcpEvent => tcpEvent.Data != null)
                .Where(tcpEvent => ((Packet)tcpEvent.Data).MessageType == MunchkinMessageType.UpdatePlayers)
                .Do(tcpEvent => _gameLogger.Debug($"Recieved {MunchkinMessageType.UpdatePlayers} message"))
                .Select(MapToPlayerInfo)
                .Subscribe(
                    UpdatePlayers,
                    error => _gameLogger.Error($"Error during broadcast host: {error.Message}")
                );
        }

        private List<PlayerInfo> MapToPlayerInfo(TcpEvent tcpEvent)
        {
            var packet = (Packet)tcpEvent.Data;
            var position = 3;
            var players = new List<PlayerInfo>();
            var playersCount = packet.Buffer[position++];
            for (byte i = 0; i < playersCount; i++)
            {
                var p = new PlayerInfo();

                p.Id = Encoding.UTF8.GetString(packet.Buffer, position + 1, packet.Buffer[position]);
                position += packet.Buffer[position];
                position++;

                p.Name = Encoding.UTF8.GetString(packet.Buffer, position + 1, packet.Buffer[position]);
                position += packet.Buffer[position];
                position++;

                p.Sex = packet.Buffer[position++];
                p.Level = packet.Buffer[position++];
                p.Modifiers = packet.Buffer[position++];
                players.Add(p);
            }
            return players;
        }

        private void UpdatePlayers(List<PlayerInfo> updatedPlayers)
        {
            var indexesToDelete = new List<int>();
            for (var i = 0; i < Players.Count; i++)
            {
                if (!updatedPlayers.Any(p => p.Id == Players[i].Id))
                {
                    indexesToDelete.Add(i);
                }
            }
            indexesToDelete.ForEach(i => Players.RemoveAt(i));

            foreach (var updatedPlayer in updatedPlayers)
            {
                var p = Players.FirstOrDefault(pl => pl.Id == updatedPlayer.Id);
                if (p != null)
                {
                    p.Name = updatedPlayer.Name;
                    p.Sex = updatedPlayer.Sex;
                    p.Level = updatedPlayer.Level;
                    p.Modifiers = updatedPlayer.Modifiers;
                }
                else
                {
                    Players.Add(new Player
                    {
                        Id = updatedPlayer.Id,
                        Name = updatedPlayer.Name,
                        Sex = updatedPlayer.Sex,
                        Level = updatedPlayer.Level,
                        Modifiers = updatedPlayer.Modifiers
                    });
                }
            }

            MessagingCenter.Send<IGameClient>(this, "PlayersUpdated");
        }

        public void StartSearchHosts()
        {
            _lanClient.StartListeningBroadcast();

            _hostsSearchSubscribe = _lanClient.PacketSubject.AsObservable()
                .TakeUntil(_destroy)
                .Where(tcpEvent => tcpEvent.Type == TcpEventType.ReceiveData)
                .Where(tcpEvent => tcpEvent.Data != null)
                .Where(tcpEvent => ((Packet)tcpEvent.Data).MessageType == MunchkinMessageType.HostFound)
                .Finally(() => _gameLogger.Debug("Game host observable end."))
                .Select(tcpEvent =>
                {
                    var packet = (Packet)tcpEvent.Data;
                    var position = 3;
                    var host = new MunchkinHost();
                    host.IpAddress = packet.SenderIpAdress;

                    host.Id = Encoding.UTF8.GetString(packet.Buffer, position + 1, packet.Buffer[position]);
                    position += packet.Buffer[position];
                    position++;

                    host.Name = Encoding.UTF8.GetString(packet.Buffer, position + 1, packet.Buffer[position]);
                    position += packet.Buffer[position];
                    position++;

                    host.Capacity = packet.Buffer[position++];
                    host.Fullness = packet.Buffer[position++];

                    _gameLogger.Debug($"Got new packet with ip [{packet.SenderIpAdress}]");
                    return host;
                })
                .Subscribe(host =>
                {
                    if (!Hosts.Any(h => h.Id == host.Id))
                    {
                        _gameLogger.Debug($"Added new host name[{host.Name}]");
                        Hosts.Add(host);
                    }
                    else
                    {
                        var hostToUpdate = Hosts.First(h => h.Id == host.Id);
                        hostToUpdate.Name = host.Name;
                        hostToUpdate.Capacity = host.Capacity;
                        hostToUpdate.Fullness = host.Fullness;
                    }

                    MessagingCenter.Send<IGameClient>(this, "HostsUpdated");
                });
        }

        public void StopSearchHosts()
        {
            _hostsSearchSubscribe?.Dispose();
            _lanClient.StopListeningBroadcast();
        }
    }
}
