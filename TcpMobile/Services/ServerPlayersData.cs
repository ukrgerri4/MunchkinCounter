using GameMunchkin.Models;
using Infrastracture.Interfaces.GameMunchkin;
using Infrastracture.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using TcpMobile.Game.Models;
using TcpMobile.Tcp.Enums;

namespace TcpMobile.Services
{
    public class ServerPlayersData
    {
        private readonly ILanServer _gameServer;
        private readonly IMultiPlayerService<Player> _multiPlayerService;

        public ConcurrentDictionary<string, PlayerInfo> KnownPlayers { get; set; } = new ConcurrentDictionary<string, PlayerInfo>();
        public ConcurrentDictionary<string, PlayerInfo> PlayersInGame { get; set; } = new ConcurrentDictionary<string, PlayerInfo>();

        private Subject<Unit> _updatePlayersSubject = new Subject<Unit>();

        public ServerPlayersData(ILanServer gameServer, IMultiPlayerService<Player> multiPlayerService)
        {
            _gameServer = gameServer;
            _multiPlayerService = multiPlayerService;

            //_updatePlayersSubject.AsObservable()
            //    .Throttle(TimeSpan.FromMilliseconds(500))
            //    .Subscribe(_ =>
            //    {
            //        var players = PlayersInGame.Values.ToList();

            //        using (MemoryStream memoryStream = new MemoryStream())
            //        {
            //            memoryStream.Write(BitConverter.GetBytes((ushort)0), 0, 2);
            //            memoryStream.WriteByte((byte)MunchkinMessageType.UpdatePlayers);
            //            memoryStream.WriteByte((byte)players.Count);

            //            foreach (var player in players)
            //            {
            //                var byteId = Encoding.UTF8.GetBytes(player.Id ?? string.Empty);
            //                memoryStream.WriteByte((byte)byteId.Length);
            //                memoryStream.Write(byteId, 0, byteId.Length);

            //                var byteName = Encoding.UTF8.GetBytes(player.Name ?? string.Empty);
            //                memoryStream.WriteByte((byte)byteName.Length);
            //                memoryStream.Write(byteName, 0, byteName.Length);

            //                memoryStream.WriteByte(player.Level);
            //                memoryStream.WriteByte(player.Modifiers);
            //            }

            //            memoryStream.Seek(0, SeekOrigin.Begin);
            //            memoryStream.Write(BitConverter.GetBytes((ushort)memoryStream.Length), 0, 2);
            //            memoryStream.Seek(0, SeekOrigin.End);

            //            var message = memoryStream.ToArray();

            //            foreach (var player in players)
            //            {
            //                _gameServer.SendMessage(player.Id, message);
            //            }
            //            _multiPlayerService.UpdatePlayers(players);
            //        }
            //    });

            //_gameServer.PacketSubject.AsObservable()
            //    .Where(tcpEvent => tcpEvent.Type == TcpEventType.ReceiveData)
            //    .Where(tcpEvent => tcpEvent.Data != null)
            //    .Where(tcpEvent => tcpEvent.Data.MessageType == MunchkinMessageType.InitInfo ||
            //        tcpEvent.Data.MessageType == MunchkinMessageType.UpdatePlayerState ||
            //        tcpEvent.Data.MessageType == MunchkinMessageType.UpdatePlayerName)
            //    .Subscribe(tcpEvent =>
            //    {
            //        var packet = tcpEvent.Data;
            //        var position = 3;

            //        switch (packet.MessageType)
            //        {
            //            case MunchkinMessageType.InitInfo:
            //                var playerInfo = new PlayerInfo();

            //                playerInfo.Id = Encoding.UTF8.GetString(packet.Buffer, position + 1, packet.Buffer[position]);
            //                position += packet.Buffer[position];
            //                position++;

            //                playerInfo.Name = Encoding.UTF8.GetString(packet.Buffer, position + 1, packet.Buffer[position]);
            //                position += packet.Buffer[position];
            //                position++;

            //                playerInfo.Level = packet.Buffer[position++];
            //                playerInfo.Modifiers = packet.Buffer[position++];

            //                if (packet.SenderId != playerInfo.Id)
            //                {
            //                    // log some warning
            //                }

            //                if (!KnownPlayers.TryAdd(packet.SenderId, playerInfo))
            //                {
            //                    KnownPlayers[packet.SenderId].Name = playerInfo.Name;
            //                    KnownPlayers[packet.SenderId].Level = playerInfo.Level;
            //                    KnownPlayers[packet.SenderId].Modifiers = playerInfo.Modifiers;
            //                }

            //                if (!PlayersInGame.TryAdd(packet.SenderId, playerInfo))
            //                {
            //                    PlayersInGame[packet.SenderId].Name = playerInfo.Name;
            //                    PlayersInGame[packet.SenderId].Level = playerInfo.Level;
            //                    PlayersInGame[packet.SenderId].Modifiers = playerInfo.Modifiers;
            //                }

            //                break;
            //            case MunchkinMessageType.UpdatePlayerState:
            //                var level = packet.Buffer[position++];
            //                var modifiers = packet.Buffer[position++];

            //                if (KnownPlayers.ContainsKey(packet.SenderId))
            //                {
            //                    KnownPlayers[packet.SenderId].Level = level;
            //                    KnownPlayers[packet.SenderId].Modifiers = modifiers;
            //                }

            //                if (PlayersInGame.ContainsKey(packet.SenderId))
            //                {
            //                    PlayersInGame[packet.SenderId].Level = level;
            //                    PlayersInGame[packet.SenderId].Modifiers = modifiers;
            //                }

            //                break;
            //            case MunchkinMessageType.UpdatePlayerName:
            //                var name = Encoding.UTF8.GetString(packet.Buffer, position + 1, packet.Buffer[position]);
            //                if (KnownPlayers.ContainsKey(packet.SenderId))
            //                {
            //                    KnownPlayers[packet.SenderId].Name = name;
            //                }

            //                if (PlayersInGame.ContainsKey(packet.SenderId))
            //                {
            //                    PlayersInGame[packet.SenderId].Name = name;
            //                }
            //                break;
            //        }

            //        _updatePlayersSubject.OnNext(Unit.Default);
            //    });
        }
    }
}
