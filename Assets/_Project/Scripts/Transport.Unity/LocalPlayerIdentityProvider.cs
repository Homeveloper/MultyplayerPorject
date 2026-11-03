using Game.Core;
using UnityEngine;

namespace Game.Transport.Unity
{
    /// <summary>
    /// IP-mode identity: a GUID generated once per process (NOT persisted via PlayerPrefs).
    /// PlayerPrefs on Windows/Mac/Linux live in one registry/file keyed by company+product
    /// name, shared by every running instance of the same build - two copies of the game
    /// on the same machine (two Standalone builds, or Multiplayer Play Mode's virtual
    /// players) would read back the exact same "persisted" GUID and get rejected as
    /// duplicate players. The duplicate-player check only needs uniqueness for the
    /// lifetime of this process, so a per-process id sidesteps the collision entirely.
    /// </summary>
    public class LocalPlayerIdentityProvider : IPlayerIdentityProvider
    {
        const string NameKey = "Game.LocalPlayerName";

        static readonly PlayerId ProcessPlayerId = new(System.Guid.NewGuid().ToString("N"));

        public PlayerId GetLocalPlayerId() => ProcessPlayerId;

        public string GetLocalPlayerName()
        {
            if (PlayerPrefs.HasKey(NameKey))
                return PlayerPrefs.GetString(NameKey);
            return $"Player{Random.Range(1000, 9999)}";
        }

        public static void SetLocalPlayerName(string name)
        {
            PlayerPrefs.SetString(NameKey, name);
            PlayerPrefs.Save();
        }
    }
}
