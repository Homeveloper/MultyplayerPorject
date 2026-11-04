namespace Game.Core
{
    // IP mode: GUID. Steam mode: SteamID + persona name. Used for dupe checks on approval.
    public interface IPlayerIdentityProvider
    {
        PlayerId GetLocalPlayerId();
        string GetLocalPlayerName();
    }
}
