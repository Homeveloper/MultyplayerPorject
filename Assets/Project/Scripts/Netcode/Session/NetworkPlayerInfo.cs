using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace Game.Netcode
{
    public struct NetworkPlayerInfo : INetworkSerializable, IEquatable<NetworkPlayerInfo>
    {
        public ulong ClientId;
        public FixedString64Bytes PlayerId;
        public FixedString64Bytes Name;
        public Color32 Color;
        public bool IsHost;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref PlayerId);
            serializer.SerializeValue(ref Name);
            serializer.SerializeValue(ref Color);
            serializer.SerializeValue(ref IsHost);
        }

        public bool Equals(NetworkPlayerInfo other) =>
            ClientId == other.ClientId && PlayerId.Equals(other.PlayerId) &&
            Name.Equals(other.Name) && Color.Equals(other.Color) && IsHost == other.IsHost;
    }
}
