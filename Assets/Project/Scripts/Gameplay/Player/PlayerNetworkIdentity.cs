using Game.Core;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Game.Gameplay.Player
{
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
            {
                DisplayName.Value = $"Player {OwnerClientId}";
                Color.OnValueChanged += OnServerColorChanged;

                // same PlayerId reconnecting within this session (not a brand new player) gets
                // their last chosen color back instead of starting over at the default. Color has
                // Owner write permission, so the server can't just assign Color.Value itself here -
                // it has to ask the owning client to set it, same as a manual color-change hotkey
                if (ActiveSession.Current != null && ActiveSession.Current.TryGet(OwnerClientId, out var info)
                    && info.Id.IsValid && ActiveSession.Current.TryGetLastKnownColor(info.Id.Value, out var savedColor))
                {
                    var target = new RpcParams { Send = new RpcSendParams { Target = RpcTarget.Single(OwnerClientId, RpcTargetUse.Temp) } };
                    RestoreColorRpc(savedColor, target);
                }
            }

            _propertyBlock = new MaterialPropertyBlock();
            Color.OnValueChanged += OnColorChanged;
            ApplyColor(Color.Value);
        }

        public override void OnNetworkDespawn()
        {
            Color.OnValueChanged -= OnColorChanged;
            if (IsServer) Color.OnValueChanged -= OnServerColorChanged;
        }

        [Rpc(SendTo.SpecifiedInParams)]
        void RestoreColorRpc(UnityEngine.Color color, RpcParams rpcParams = default)
        {
            if (IsOwner) Color.Value = color;
        }

        void OnServerColorChanged(UnityEngine.Color previous, UnityEngine.Color current)
        {
            if (ActiveSession.Current != null && ActiveSession.Current.TryGet(OwnerClientId, out var info) && info.Id.IsValid)
                ActiveSession.Current.RememberColor(info.Id.Value, current);
        }

        void OnColorChanged(UnityEngine.Color previous, UnityEngine.Color current) => ApplyColor(current);

        void ApplyColor(UnityEngine.Color color)
        {
            if (_visualRenderer == null) return;
            _visualRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor("_Color", color);
            _propertyBlock.SetColor("_BaseColor", color); // URP, in case the project's pipeline changes later
            _visualRenderer.SetPropertyBlock(_propertyBlock);
        }

        // owner writes directly, no RPC needed - NetworkVariable already has owner write permission
        public void RequestColorChange(UnityEngine.Color color)
        {
            if (IsOwner) Color.Value = color;
        }
    }
}
