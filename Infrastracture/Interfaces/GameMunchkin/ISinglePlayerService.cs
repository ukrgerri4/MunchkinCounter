using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Text;

namespace Infrastracture.Interfaces.GameMunchkin
{
    public interface ISinglePlayerService<TPlayer>
    {
        Subject<TPlayer> PlayerSubject { get; }
        TPlayer GetPlayer();
    }
}
