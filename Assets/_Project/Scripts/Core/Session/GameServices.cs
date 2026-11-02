namespace Game.Core
{
    /// <summary>
    /// Minimal resolver abstraction. NGO instantiates NetworkBehaviours itself (via its
    /// spawn system), so they cannot be constructor-injected by VContainer like regular
    /// services; this is the one documented escape hatch they use in OnNetworkSpawn to
    /// pull Core-level dependencies. CompositionRoot sets <see cref="Current"/> to a
    /// thin adapter over the real DI container - everything else still goes through
    /// proper constructor injection.
    /// </summary>
    public interface IServiceLocator
    {
        T Resolve<T>();
    }

    public static class GameServices
    {
        public static IServiceLocator Current { get; set; }
    }
}
