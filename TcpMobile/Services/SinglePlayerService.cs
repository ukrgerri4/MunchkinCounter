using GameMunchkin.Models;
using Infrastracture.Interfaces.GameMunchkin;
using System.Reactive.Subjects;

namespace TcpMobile.Services
{
    public class SinglePlayerService : ISinglePlayerService<Player>
    {
        private Subject<Player> _playerSubject = new Subject<Player>();
        public Subject<Player> PlayerSubject => _playerSubject;

        private Player _player = new Player();

        public SinglePlayerService()
        {
        }

        public Player GetPlayer()
        {
            return _player;
        }
    }
}
