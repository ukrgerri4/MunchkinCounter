using GameMunchkin.Models;
using Infrastracture.Interfaces.GameMunchkin;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Subjects;

namespace TcpMobile.Services
{
    public class MultiPlayerService : IMultiPlayerService<Player>
    {
        private Subject<List<Player>> _playersSubject = new Subject<List<Player>>();
        public Subject<List<Player>> PlayersSubject => _playersSubject;

        private ObservableCollection<Player> _players = new ObservableCollection<Player>();

        public MultiPlayerService()
        {
        }

        public ObservableCollection<Player> GetPlayers()
        {
            return _players;
        }
    }
}
