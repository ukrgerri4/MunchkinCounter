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
        //Subject<List<TPlayer>> PlayersSubject { get; }
        ObservableCollection<TPlayer> GetPlayers();
        void StartUpdatePlayers();
        void UpdatePlayers(List<PlayerInfo> updatedPlayers);
    }
}
