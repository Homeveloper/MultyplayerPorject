namespace Game.Core
{
    /// <summary>
    /// Where the local player's persistent identity and display name come from.
    /// IP mode: a GUID persisted locally (PlayerPrefs). Steam mode: the SteamID
    /// and Steam persona name. Used for duplicate-player checks on approval.
    /// </summary>
    public interface IPlayerIdentityProvider
    {
        PlayerId GetLocalPlayerId();
        string GetLocalPlayerName();
    }
}
