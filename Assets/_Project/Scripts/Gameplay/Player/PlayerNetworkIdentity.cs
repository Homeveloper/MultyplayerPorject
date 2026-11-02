using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Game.Gameplay.Player
{
    /// <summary>Replicated identity/appearance of a player - name (server-set) and color (owner-settable, bonus feature).</summary>
    [RequireComponent(typeof(NetworkObject))]
    public class PlayerNetworkIdentity : NetworkBehaviour
    {
        [SerializeField] Renderer _visualRenderer;

        public NetworkVariable<FixedString64Bytes> DisplayName = new(
            default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public NetworkVariable<Color> Color = new(
            UnityEngine.Color.white, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        MaterialPropertyBlock _propertyBlock;

        public override void OnNetworkSpawn()
        {
            if (IsServer)
                DisplayName.Value = $"Player {OwnerClientId}";

            _propertyBlock = new MaterialPropertyBlock();
            Color.OnValueChanged += OnColorChanged;
            ApplyColor(Color.Value);
        }

        public override void OnNetworkDespawn() => Color.OnValueChanged -= OnColorChanged;

        void OnColorChanged(UnityEngine.Color previous, UnityEngine.Color current) => ApplyColor(current);

        void ApplyColor(UnityEngine.Color color)
        {
            if (_visualRenderer == null) return;
            _visualRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_Color", color);
            _propertyBlock.SetColor("_BaseColor", color); // URP, in case the project's pipeline changes later
            _visualRenderer.SetPropertyBlock(_propertyBlock);
        }

        /// <summary>Hotkey-driven color change (bonus feature) - owner writes directly, no RPC needed
        /// since the NetworkVariable already has owner write permission; replicates to everyone,
        /// including late joiners, automatically.</summary>
        public void RequestColorChange(UnityEngine.Color color)
        {
            if (IsOwner) Color.Value = color;
        }
    }
}
