namespace Game.Core
{
    // one impl per TransportType - swapping transports is just a DI resolve, nothing else changes
    public interface ITransportConfigurator
    {
        TransportType Type { get; }

        void ConfigureForHost(AppConfig config);
        void ConfigureForClient(AppConfig config, string joinTarget);
    }
}
