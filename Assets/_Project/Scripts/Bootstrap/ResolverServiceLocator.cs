using Game.Core;
using VContainer;

namespace Game.Bootstrap
{
    // thin adapter so NGO-spawned objects can pull from the real container (see GameServices)
    public class ResolverServiceLocator : IServiceLocator
    {
        readonly IObjectResolver _resolver;
        public ResolverServiceLocator(IObjectResolver resolver) => _resolver = resolver;
        public T Resolve<T>() => _resolver.Resolve<T>();
    }
}
