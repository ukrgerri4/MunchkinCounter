using Infrastracture.Interfaces.GameMunchkin;

namespace Infrastracture.Interfaces
{
    public interface IGameService
    {
        IPlayer Player { get; }
        IGameServer GameServer { get; }
        IGameClient GameClient { get; }
    }
}
