using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Game.Gameplay.Player
{
    /// <summary>Replicated identity/appearance of a player - name (server-set) and color (owner-settable, bonus feature).</summary>
    [RequireComponent(typeof(NetworkObject))]
    public class PlayerNetworkIdentity : NetworkBehaviour
    {
        public NetworkVariable<FixedString64Bytes> DisplayName = new(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<Color> Color = new(
            UnityEngine.Color.white, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public override void OnNetworkSpawn()
        {
            if (IsServer)
                DisplayName.Value = $"Player {OwnerClientId}";
        }

        /// <summary>Hotkey-driven color change (bonus feature) - owner writes directly, no RPC needed
        /// since the NetworkVariable already has owner write permission; replicates to everyone,
        /// including late joiners, automatically.</summary>
        public void RequestColorChange(Color color)
        {
            if (IsOwner) Color.Value = color;
        }
    }
}
