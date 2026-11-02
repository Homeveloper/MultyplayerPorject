using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>Read-only, replicated view of everyone in the current session (lobby or in-game).</summary>
    public interface IPlayerRegistry
    {
        IReadOnlyList<PlayerInfo> Players { get; }
        event Action Changed;
        bool TryGet(ulong clientId, out PlayerInfo info);
    }
}
