using GameMunchkin.Models;
using Infrastracture.Interfaces.GameMunchkin;
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
    public class MultiPlayerService : IMultiPlayerService<Player>
    {
        private readonly IGameClient _gameClient;
        
        private ObservableCollection<Player> _players = new ObservableCollection<Player>();
        
        private Subject<Unit> _destroy = new Subject<Unit>();

        public MultiPlayerService(IGameClient gameClient)
        {
            _gameClient = gameClient;
        }

        public ObservableCollection<Player> GetPlayers()
        {
            return _players;
        }

        public void StartUpdatePlayers()
        {
            _gameClient.PacketSubject.AsObservable()
                .TakeUntil(_destroy)
                .Where(packet => packet.MessageType == MunchkinMessageType.UpdatePlayers)
                .Select(packet =>
                {
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
                })
                .Subscribe(updatedPlayers => UpdatePlayers(updatedPlayers));
        }

        public void UpdatePlayers(List<PlayerInfo> updatedPlayers)
        {
            var indexesToDelete = new List<int>();
            for (var i = 0; i < _players.Count; i++)
            {
                if (!updatedPlayers.Any(p => p.Id == _players[i].Id))
                {
                    indexesToDelete.Add(i);
                }
            }
            indexesToDelete.ForEach(i => _players.RemoveAt(i));

            foreach (var updatedPlayer in updatedPlayers)
            {
                var p = _players.FirstOrDefault(pl => pl.Id == updatedPlayer.Id);
                if (p != null)
                {
                    p.Name = updatedPlayer.Name;
                    p.Level = updatedPlayer.Level;
                    p.Modifiers = updatedPlayer.Modifiers;
                }
                else
                {
                    _players.Add(new Player
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
