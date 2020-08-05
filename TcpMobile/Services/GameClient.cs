using Core.Models;
using Infrastracture.Definitions;
using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using Infrastracture.Models;
using Newtonsoft.Json;
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
using Xamarin.Essentials;
using Xamarin.Forms;

namespace TcpMobile.Services
{
    public class GameClient : IGameClient
    {
        private IGameLogger _gameLogger => DependencyService.Get<IGameLogger>();
        private ILanClient _lanClient => DependencyService.Get<ILanClient>();
        private IDeviceInfoService _deviceInfoService => DependencyService.Get<IDeviceInfoService>();

        public ObservableCollection<MunchkinHost> Hosts { get; set; }
        public Player MyPlayer { get; set; }
        public List<Player> Players { get; set; }

        
        private IDisposable _hostsSearchSubscription;

        private Subject<Unit> _destroy = new Subject<Unit>();

        public GameClient()
        {
            Hosts = new ObservableCollection<MunchkinHost>();

            MyPlayer = new Player
            {
                Id = _deviceInfoService.DeviceId,
                Name = Preferences.Get(PreferencesKey.DefaultPlayerName, "Player-1"),
                Sex = (byte)Preferences.Get(PreferencesKey.DefaultPlayerSex, 0)
            };
            Players = new List<Player>();
        }

        public Result Connect(IPAddress ip)
        {
            return _lanClient.Connect(ip);
        }

        public Result CloseConnection()
        {
            try
            {
                _destroy.OnNext(Unit.Default);
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
                return Result.Fail($"GameClient stop error: {e.Message}");
            }
        }

        public void ConnectSelf()
        {
            var localIPs = Dns.GetHostAddresses(Dns.GetHostName());

            _gameLogger.Debug($"GameClient ConnectSelf connections => {string.Join(",", localIPs.Select(ip => ip.ToString()))}");

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
                    _gameLogger.Error(initMessageResult.Error);
                    return initMessageResult;
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
                    _gameLogger.Error(initMessageResult.Error);
                    return initMessageResult;
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
                    _gameLogger.Error(initMessageResult.Error);
                    return initMessageResult;
                }

                return Result.Ok();
            }
        }

        public void StartUpdatePlayers()
        {
            _lanClient.TcpClientEventSubject.AsObservable()
                .TakeUntil(_destroy)
                .Where(tcpEvent => tcpEvent.Type == TcpEventType.ReceiveData)
                .Where(tcpEvent => tcpEvent.Data != null)
                .Where(tcpEvent => ((Packet)tcpEvent.Data).MessageType == MunchkinMessageType.UpdatePlayers)
                .Select(MapToPlayerInfo)
                .Subscribe(
                    UpdatePlayers,
                    error => _gameLogger.Error($"Error during StartUpdatePlayers SUBSCRIPTION: {error.Message}")
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

            _hostsSearchSubscription = _lanClient.TcpClientEventSubject.AsObservable()
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

                    _gameLogger.Debug($"GameClient: got new packet with ip [{packet.SenderIpAdress}]");
                    return host;
                })
                .Subscribe(host =>
                {
                    if (!Hosts.Any(h => h.Id == host.Id))
                    {
                        _gameLogger.Debug($"GameClient: added new host name[{host.Name}]");
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

        public void StartListeningServerDisconnection()
        {
            _lanClient.TcpClientEventSubject.AsObservable()
                .TakeUntil(_destroy)
                .Where(tcpEvent => tcpEvent.Type == TcpEventType.StopServerConnection)
                .Do(tcpEvent => _gameLogger.Debug($"GameClient: server disconnected handler"))
                .Subscribe(
                    tcpEvent =>
                    {
                        Hosts.Clear();
                        Players.Clear();
                        SavePlayerData();
                        MessagingCenter.Send<IGameClient>(this, "LostServerConnection");
                    },
                    error =>
                    {
                        _gameLogger.Error($"GameClient: error during listening for server disconnection: {error.Message}");
                    }
                );
        }

        public void StopSearchHosts()
        {
            _hostsSearchSubscription?.Dispose();
            _lanClient.StopListeningBroadcast();
        }

        public void SavePlayerData()
        {
            var playerInfo = new PlayerInfo
            {
                Id = MyPlayer.Id,
                Name = MyPlayer.Name,
                Level = MyPlayer.Level,
                Modifiers = MyPlayer.Modifiers,
                Sex = MyPlayer.Sex
            };
            var serializedPlayer = JsonConvert.SerializeObject(playerInfo);
            Preferences.Set(PreferencesKey.LastPlayerData, serializedPlayer);
        }

        public void RestorePlayerData()
        {
            try
            {
                var serializedPlayer = Preferences.Get(PreferencesKey.LastPlayerData, null);
                if (serializedPlayer != null)
                {
                    var player = JsonConvert.DeserializeObject<Player>(serializedPlayer);
                    MyPlayer.Name = player.Name;
                    MyPlayer.Level = player.Level;
                    MyPlayer.Modifiers = player.Modifiers;
                    MyPlayer.Sex = player.Sex;
                }
            }
            catch (Exception e)
            {
                _gameLogger.Error($"GameClient RestorePlayerData: {e.Message}");
            }
            
        }
    }
}
