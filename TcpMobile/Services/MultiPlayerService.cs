using GameMunchkin.Models;
using Infrastracture.Interfaces.GameMunchkin;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace TcpMobile.Services
{
    public class MultiPlayerService : IMultiPlayerService<Player>
    {
        private Subject<List<Player>> _playersSubject;
        public Subject<List<Player>> PlayersSubject => _playersSubject;

        private ObservableCollection<Player> _players = new ObservableCollection<Player>();

        private Subject<Unit> _destroy = new Subject<Unit>();

        public MultiPlayerService()
        {
            _playersSubject = new Subject<List<Player>>();
            _playersSubject.AsObservable()
                .TakeUntil(_destroy)
                .Throttle(TimeSpan.FromMilliseconds(500))
                .Where(p => p.Any())

        }

        public ObservableCollection<Player> GetPlayers()
        {
            return _players;
        }
    }
}
