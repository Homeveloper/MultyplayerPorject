using System;
using System.Collections.Generic;

namespace Game.Core
{
    public interface IPlayerRegistry
    {
        IReadOnlyList<PlayerInfo> Players { get; }
        event Action Changed;
        bool TryGet(ulong clientId, out PlayerInfo info);
    }
}
