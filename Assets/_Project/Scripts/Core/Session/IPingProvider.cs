namespace Game.Core
{
    public interface IPingProvider
    {
        float GetRttMilliseconds(ulong clientId);
    }
}
