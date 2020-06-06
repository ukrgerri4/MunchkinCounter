using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Subjects;
using System.Text;
using TcpMobile.Game.Models;

namespace Infrastracture.Interfaces.GameMunchkin
{
    public interface IMultiPlayerService<TPlayer>
    {
        ObservableCollection<TPlayer> Players { get; }
        void StartUpdatePlayers();
        void UpdatePlayers(List<PlayerInfo> updatedPlayers);
    }
}
