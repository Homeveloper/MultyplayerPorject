namespace Game.Core
{
    /// <summary>Round-trip time lookup, one implementation per transport.</summary>
    public interface IPingProvider
    {
        float GetRttMilliseconds(ulong clientId);
    }
}
