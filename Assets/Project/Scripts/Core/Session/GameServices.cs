namespace Game.Core
{
    // NGO spawns NetworkBehaviours itself so VContainer can't constructor-inject them -
    // this is their one escape hatch to pull dependencies in OnNetworkSpawn.
    public interface IServiceLocator
    {
        T Resolve<T>();
    }

    public static class GameServices
    {
        public static IServiceLocator Current { get; set; }
    }
}
