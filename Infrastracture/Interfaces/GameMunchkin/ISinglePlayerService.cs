﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastracture.Interfaces.GameMunchkin
{
    public interface ISinglePlayerService<T>
    {
        T GetLastValue();
    }
}
