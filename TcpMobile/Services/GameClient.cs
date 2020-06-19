using GameMunchkin.Models;
using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using Infrastracture.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using TcpMobile.Game.Models;
using TcpMobile.Tcp.Enums;

namespace TcpMobile.Services
{
    public class GameClient
    {
        private readonly IGameLogger _gameLogger;
        private readonly IConfiguration _configuration;
        private readonly ILanClient _lanClient;

        private Subject<Unit> _destroy = new Subject<Unit>();
        
        public ObservableCollection<Player> Players { get; set; } = new ObservableCollection<Player>();

        public GameClient(
            IGameLogger gameLogger,
            IConfiguration configuration,
            ILanClient lanClient
         )
        {
            _gameLogger = gameLogger;
            _configuration = configuration;
            _lanClient = lanClient;
        }

        private void StartUpdatePlayers()
        {
            _lanClient.PacketSubject.AsObservable()
                .TakeUntil(_destroy)
                .Where(tcpEvent => tcpEvent.Type == TcpEventType.ReceiveData)
                .Where(tcpEvent => tcpEvent.Data != null)
                .Where(tcpEvent => tcpEvent.Data.MessageType == MunchkinMessageType.UpdatePlayers)
                .Do(tcpEvent => _gameLogger.Debug($"Recieved {MunchkinMessageType.UpdatePlayers} message"))
                .Select(MapToPlayerInfo)
                .Subscribe(
                    UpdatePlayers,
                    error => _gameLogger.Error($"Error during broadcast host: {error.Message}")
                );
        }

        private List<PlayerInfo> MapToPlayerInfo(TcpEvent tcpEvent)
        {
            var packet = tcpEvent.Data;
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
                    p.Level = updatedPlayer.Level;
                    p.Modifiers = updatedPlayer.Modifiers;
                }
                else
                {
                    Players.Add(new Player
                    {
                        Id = updatedPlayer.Id,
                        Name = updatedPlayer.Name,
                        Level = updatedPlayer.Level,
                        Modifiers = updatedPlayer.Modifiers
                    });
                }

            }
        }
    }
}
