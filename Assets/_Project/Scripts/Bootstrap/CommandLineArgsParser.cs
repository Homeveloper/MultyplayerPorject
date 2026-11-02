using System;
using Game.Core;

namespace Game.Bootstrap
{
    /// <summary>Parses `-transport steam|ip`, `-port <n>`, `-ip <address>` so transport
    /// choice can be overridden per-launch without touching AppConfig.</summary>
    public static class CommandLineArgsParser
    {
        public static void ApplyOverrides(AppConfig config)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "-transport":
                        var v = args[i + 1].ToLowerInvariant();
                        if (v is "steam" or "steamp2p") config.TransportTypeOverride = TransportType.SteamP2P;
                        else if (v is "ip" or "unity" or "unitytransport") config.TransportTypeOverride = TransportType.UnityTransport;
                        break;
                    case "-port":
                        if (ushort.TryParse(args[i + 1], out var port)) config.PortOverride = port;
                        break;
                    case "-ip":
                        config.JoinAddressOverride = args[i + 1];
                        break;
                }
            }
        }
    }
}
