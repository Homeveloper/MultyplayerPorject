using Unity.Netcode.Components;

namespace Game.Netcode
{
    // owner-authoritative NetworkTransform for the Player prefab
    public class OwnerNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative() => false;
    }
}
