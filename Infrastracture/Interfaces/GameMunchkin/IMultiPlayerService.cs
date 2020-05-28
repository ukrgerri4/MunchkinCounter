using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive.Subjects;
using System.Text;

namespace Infrastracture.Interfaces.GameMunchkin
{
    public interface IMultiPlayerService<TPlayer>
    {
        Subject<List<TPlayer>> PlayersSubject { get; }
        ObservableCollection<TPlayer> GetPlayers();
    }
}
