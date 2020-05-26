using Infrastracture.Interfaces;
using Infrastracture.Interfaces.GameMunchkin;
using System;

namespace TcpMobile
{
    public class SocketService : IGameService
    {
        private IPlayer _player;
        private IGameServer _gameServer;
        private IGameClient _gameClient;

        public IGameServer GameServer =>_gameServer ?? throw new ArgumentNullException(nameof(_gameServer));
        public IGameClient GameClient => _gameClient ?? throw new ArgumentNullException(nameof(_gameClient));
        public IPlayer Player => _player ?? throw new ArgumentNullException(nameof(_player));

        public SocketService(IGameServer gameServer, IGameClient gameClient, IPlayer player)
        {
            _player = player;
            _gameServer = gameServer;
           _gameClient = gameClient;
        }
    }
}
