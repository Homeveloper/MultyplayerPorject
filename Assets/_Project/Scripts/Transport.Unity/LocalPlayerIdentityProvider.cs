using Game.Core;
using UnityEngine;

namespace Game.Transport.Unity
{
    // GUID per process, not persisted in PlayerPrefs - PlayerPrefs is shared across every
    // instance of the same build on one machine, so two copies running locally would read the
    // same "persisted" GUID and get bounced as duplicate players.
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
