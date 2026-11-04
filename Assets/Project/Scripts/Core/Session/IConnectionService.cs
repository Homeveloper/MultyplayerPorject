using System;

namespace Game.Core
{
    public readonly struct ConnectRequest
    {
        public readonly string Address;
        public readonly ushort Port;
        public ConnectRequest(string address, ushort port) { Address = address; Port = port; }
    }

    // transport-agnostic facade - gameplay code never touches NetworkManager/transport directly
    public interface IConnectionService
    {
        bool IsHost { get; }
        bool IsClient { get; }
        bool IsConnected { get; }

        void StartHost();
        void StartClient(ConnectRequest request);
        void Shutdown();

        // host-only, synced scene load - a plain SceneManager.LoadScene would destroy spawned NetworkObjects
        void HostLoadScene(string sceneName);

        // host-only: disconnects the client and remembers them for this session, so a later
        // reconnect attempt gets rejected at approval instead of just letting them back in
        void KickClient(ulong clientId);

        // client-only: called from the kick RPC, before the host actually severs the connection -
        // the Steam P2P transport can close the connection before NGO's own disconnect-reason
        // message arrives, so this lets the kicked client show the right reason regardless
        void NoteLocalKickReceived();

        event Action<ulong> ClientConnected;
        event Action<ulong, string> ClientDisconnected; // clientId, reason
        event Action<string> LocalDisconnected; // reason shown to this client (e.g. host left, rejected)
    }
}
