using System;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Game.Gameplay.Items
{
    /// <summary>
    /// A grabbable physics prop. Host validates every interaction (distance, free/held
    /// state, "is it actually your item"); ownership moves to the holder while carried
    /// (so the holder's NetworkTransform drives its visual position with no extra
    /// latency for them) and returns to the server on drop/throw so the flight and
    /// landing are simulated once, on the host, and replicated identically to everyone -
    /// no per-client re-simulation, no teleport on landing.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NetworkTransform))]
    public class PhysicsItem : NetworkBehaviour
    {
        [SerializeField] float _maxPickupDistance = 3f;

        public NetworkVariable<bool> IsHeld = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<ulong> HolderClientId = new(ulong.MaxValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        Rigidbody _rigidbody;
        NetworkTransform _networkTransform;

        void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _networkTransform = GetComponent<NetworkTransform>();
        }

        /// <summary>Server-only: validates and performs a pickup. Returns false (no state change,
        /// no desync) if the item was already held or the requester is out of range.</summary>
        public bool ServerTryPickup(ulong requesterClientId)
        {
            if (!IsServer) return false;
            if (IsHeld.Value) return false; // race: whoever's request lands first on the server wins

            if (NetworkManager.ConnectedClients.TryGetValue(requesterClientId, out var client) && client.PlayerObject != null)
            {
                var dist = Vector3.Distance(client.PlayerObject.transform.position, transform.position);
                if (dist > _maxPickupDistance) return false;
            }

            NetworkObject.ChangeOwnership(requesterClientId);
            IsHeld.Value = true;
            HolderClientId.Value = requesterClientId;

            _rigidbody.isKinematic = true;
            _networkTransform.InLocalSpace = false;
            return true;
        }

        /// <summary>Server-only: the holder let go without throwing - falls where it is.</summary>
        public void ServerRelease()
        {
            if (!IsServer || !IsHeld.Value) return;
            ReturnToServerAuthority();
        }

        /// <summary>Server-only: applies the throw velocity. Simulated only here, on the host, so
        /// every client sees the same trajectory and landing point.</summary>
        public void ServerThrow(Vector3 velocity)
        {
            if (!IsServer || !IsHeld.Value) return;
            ReturnToServerAuthority();
            _rigidbody.linearVelocity = velocity;
        }

        void ReturnToServerAuthority()
        {
            IsHeld.Value = false;
            HolderClientId.Value = ulong.MaxValue;
            _rigidbody.isKinematic = false;
            if (OwnerClientId != NetworkManager.ServerClientId)
                NetworkObject.ChangeOwnership(NetworkManager.ServerClientId);
        }

        /// <summary>Server-only: called when the holder disconnects mid-hold - drop in place instead of
        /// leaving the item stuck "held" by a client that no longer exists.</summary>
        public void ServerForceReleaseIfHeldBy(ulong clientId)
        {
            if (IsServer && IsHeld.Value && HolderClientId.Value == clientId)
                ReturnToServerAuthority();
        }

        /// <summary>Fired locally when this client gains/loses ownership of an item (i.e. picks
        /// it up / lets go of it), so the owning <c>PlayerItemHolder</c> knows to start/stop
        /// driving its transform toward the hand anchor each frame.</summary>
        public static event Action<PhysicsItem> OwnershipGainedLocally;
        public static event Action<PhysicsItem> OwnershipLostLocally;

        public override void OnGainedOwnership() => OwnershipGainedLocally?.Invoke(this);
        public override void OnLostOwnership() => OwnershipLostLocally?.Invoke(this);
    }
}
