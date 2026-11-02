using Game.Core;
using Game.Gameplay.Session;
using Game.Netcode;
using Game.Transport.Steam;
using Game.Transport.Unity;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Game.Bootstrap
{
    /// <summary>
    /// The one place that decides which transport/identity/lobby/stats implementation
    /// gets wired up, based on <see cref="AppConfig"/> (and its CLI overrides). Lives on
    /// a persistent object in the Bootstrap scene, which is loaded first and never unloaded.
    /// </summary>
    public class CompositionRoot : LifetimeScope
    {
        [SerializeField] AppConfig _config;
        [SerializeField] NetworkManager _networkManager;

        SteamBootstrap _steamBootstrap;

        protected override void Awake()
        {
            CommandLineArgsParser.ApplyOverrides(_config);

            if (_config.ResolvedTransportType == TransportType.SteamP2P)
            {
                _steamBootstrap = new SteamBootstrap();
                if (!_steamBootstrap.TryInit(_config.SteamAppId))
                {
                    Debug.LogWarning("[Bootstrap] Steam unavailable - falling back to Unity Transport.");
                    _config.TransportTypeOverride = TransportType.UnityTransport;
                }
            }

            base.Awake(); // builds the container via Configure() below
            GameServices.Current = new ResolverServiceLocator(Container);

            var flow = new GameObject("SessionFlowController").AddComponent<SessionFlowController>();
            DontDestroyOnLoad(flow.gameObject);
            flow.Construct(Container.Resolve<IConnectionService>());
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_config);
            builder.RegisterInstance(_networkManager);

            bool steam = _config.ResolvedTransportType == TransportType.SteamP2P;

            if (steam)
            {
                builder.RegisterInstance(_steamBootstrap);
                builder.Register<SteamTransportInstaller>(Lifetime.Singleton).As<ITransportConfigurator>();
                builder.Register<SteamPlayerIdentityProvider>(Lifetime.Singleton).As<IPlayerIdentityProvider>();
                builder.Register<SteamLobbyService>(Lifetime.Singleton).As<ILobbyService>();
                builder.Register<SteamStatsService>(Lifetime.Singleton).As<IStatsService>();
            }
            else
            {
                builder.Register<UnityTransportInstaller>(Lifetime.Singleton).As<ITransportConfigurator>();
                builder.Register<LocalPlayerIdentityProvider>(Lifetime.Singleton).As<IPlayerIdentityProvider>();
                builder.Register<NullLobbyService>(Lifetime.Singleton).As<ILobbyService>();
                builder.Register<LoggingStatsService>(Lifetime.Singleton).As<IStatsService>();
            }

            builder.Register<ApprovalHandler>(Lifetime.Singleton);
            builder.Register<NetcodeConnectionService>(Lifetime.Singleton).As<IConnectionService>();
            builder.Register<NetworkSceneLoader>(Lifetime.Singleton);
        }
    }
}
