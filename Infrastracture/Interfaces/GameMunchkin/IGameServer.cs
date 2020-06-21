using Core.Models;
using Infrastracture.Models;
using System.Collections.Concurrent;
using TcpMobile.Game.Models;

namespace Infrastracture.Interfaces.GameMunchkin
{
    public interface IGameServer
    {
        ConcurrentDictionary<string, PlayerInfo> ConnectedPlayers { get; set; }
        MunchkinHost Host { get; set; }

        Result Start();
        Result Stop();
        void StopConnectedPlayersBroadcast();
        Result StopBroadcast();
    }
}