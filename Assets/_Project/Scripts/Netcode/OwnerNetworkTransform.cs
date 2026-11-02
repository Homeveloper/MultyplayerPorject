using Unity.Netcode.Components;

namespace Game.Netcode
{
    /// <summary>Owner-authoritative NetworkTransform, used on the Player prefab: each client
    /// moves and replicates its own character directly (see PlayerController for why).</summary>
    public class OwnerNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative() => false;
    }
}
