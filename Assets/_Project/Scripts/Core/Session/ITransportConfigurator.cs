namespace Game.Core
{
    /// <summary>
    /// Configures the concrete transport (UnityTransport / Steam P2P) on the netcode
    /// stack before a host/join attempt. One implementation per <see cref="TransportType"/>;
    /// swapping transports means swapping which implementation DI resolves - no other
    /// code changes.
    /// </summary>
    public interface ITransportConfigurator
    {
        TransportType Type { get; }

        /// <summary>Configure the transport for hosting (bind address/port or create a Steam lobby transport).</summary>
        void ConfigureForHost(AppConfig config);

        /// <summary>Configure the transport for joining the given target.</summary>
        void ConfigureForClient(AppConfig config, string joinTarget);
    }
}
