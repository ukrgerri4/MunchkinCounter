using GameMunchkin.Models;
using Infrastracture.Interfaces.GameMunchkin;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using TcpMobile.Game.Models;
using TcpMobile.Tcp.Enums;

namespace TcpMobile.Services
{
    public class GameServer
    {
        private readonly IGameServer _gameServer;

        public Dictionary<string, PlayerInfo> KnownPlayers { get; set; } = new Dictionary<string, PlayerInfo>();
        public Dictionary<string, PlayerInfo> PlayersInGame { get; set; } = new Dictionary<string, PlayerInfo>();

        public GameServer(IGameServer gameServer)
        {
            _gameServer = gameServer;
            _gameServer.PacketSubject.AsObservable()
                .Where(packet => packet.MessageType == MunchkinMessageType.InitInfo ||
                    packet.MessageType == MunchkinMessageType.UpdatePlayerState ||
                    packet.MessageType == MunchkinMessageType.UpdatePlayerName)
                .Subscribe(packet =>
                {
                    var position = 3;
                    var playerInfo = new PlayerInfo();

                    switch (packet.MessageType)
                    {
                        case MunchkinMessageType.InitInfo:
                            playerInfo.Level = packet.Buffer[position++];
                            playerInfo.Modifiers = packet.Buffer[position++];

                            playerInfo.Id = Encoding.UTF8.GetString(packet.Buffer, position + 1, packet.Buffer[position]);
                            position += packet.Buffer[position];
                            position++;

                            playerInfo.Name = Encoding.UTF8.GetString(packet.Buffer, position + 1, packet.Buffer[position]);

                            if (!KnownPlayers.ContainsKey(playerInfo.Id))
                                KnownPlayers.Add(playerInfo.Id, playerInfo);

                            if (!PlayersInGame.ContainsKey(playerInfo.Id))
                                PlayersInGame.Add(playerInfo.Id, playerInfo);

                            break;
                        case MunchkinMessageType.UpdatePlayerState:
                            playerInfo.Level = packet.Buffer[position++];
                            playerInfo.Modifiers = packet.Buffer[position++];


                            break;
                        case MunchkinMessageType.UpdatePlayerName:
                            playerInfo.Name = Encoding.UTF8.GetString(packet.Buffer, position + 1, packet.Buffer[position]);
                            break;
                    }
                });
        }
    }
}
