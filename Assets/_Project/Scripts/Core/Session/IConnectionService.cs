using System;

namespace Game.Core
{
    public readonly struct ConnectRequest
    {
        public readonly string Address;
        public readonly ushort Port;
        public ConnectRequest(string address, ushort port) { Address = address; Port = port; }
    }

    /// <summary>
    /// Transport-agnostic facade over the underlying netcode session.
    /// Gameplay code talks to this, never to NetworkManager/UnityTransport/Steam directly.
    /// </summary>
    public interface IConnectionService
    {
        bool IsHost { get; }
        bool IsClient { get; }
        bool IsConnected { get; }

        void StartHost();
        void StartClient(ConnectRequest request);
        void Shutdown();

        /// <summary>Host-only: synchronized scene load for every connected client (see
        /// NetworkSceneLoader) - never use a plain SceneManager.LoadScene for a scene change
        /// that must carry spawned NetworkObjects across, or they will be destroyed uncleanly.</summary>
        void HostLoadScene(string sceneName);

        event Action<ulong> ClientConnected;
        event Action<ulong, string> ClientDisconnected; // clientId, reason
        event Action<string> LocalDisconnected; // reason shown to this client (e.g. host left, rejected)
    }
}
