using Game.Core;
using UnityEngine;

namespace Game.Transport.Unity
{
    /// <summary>IP-mode identity: a GUID generated once and persisted in PlayerPrefs, plus a locally chosen name.</summary>
    public class LocalPlayerIdentityProvider : IPlayerIdentityProvider
    {
        const string GuidKey = "Game.LocalPlayerGuid";
        const string NameKey = "Game.LocalPlayerName";

        public PlayerId GetLocalPlayerId()
        {
            if (!PlayerPrefs.HasKey(GuidKey))
            {
                PlayerPrefs.SetString(GuidKey, System.Guid.NewGuid().ToString("N"));
                PlayerPrefs.Save();
            }
            return new PlayerId(PlayerPrefs.GetString(GuidKey));
        }

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
