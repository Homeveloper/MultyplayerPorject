using Game.Core;
using VContainer;

namespace Game.Bootstrap
{
    /// <summary>Thin adapter so NGO-instantiated NetworkBehaviours (see Core.GameServices) can
    /// pull dependencies out of the real VContainer container without a direct reference to it.</summary>
    public class ResolverServiceLocator : IServiceLocator
    {
        readonly IObjectResolver _resolver;
        public ResolverServiceLocator(IObjectResolver resolver) => _resolver = resolver;
        public T Resolve<T>() => _resolver.Resolve<T>();
    }
}
