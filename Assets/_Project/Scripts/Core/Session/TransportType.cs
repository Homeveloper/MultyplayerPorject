namespace Game.Core
{
    /// <summary>
    /// The one place a build decides which transport to use. Set in <c>AppConfig</c>
    /// or overridden by the <c>-transport</c> command-line argument. Nothing outside
    /// the Bootstrap/Transport.* layers should branch on this.
    /// </summary>
    public enum TransportType
    {
        UnityTransport,
        SteamP2P
    }
}
