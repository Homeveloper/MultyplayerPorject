namespace Game.Core
{
    public static class DisconnectReason
    {
        public const string SessionFull = "session_full";
        public const string VersionMismatch = "version_mismatch";
        public const string DuplicatePlayer = "duplicate_player";
        public const string ConnectionTimeout = "connection_timeout";
        public const string Kicked = "kicked";
        public const string HostLeft = "host_left";
        public const string LocalError = "local_error";

        public static string ToDisplayText(string reason) => reason switch
        {
            SessionFull => "Session is full.",
            VersionMismatch => "Your build version does not match the host's.",
            DuplicatePlayer => "This player is already connected.",
            ConnectionTimeout => "Connection timed out.",
            Kicked => "You were kicked by the host.",
            HostLeft => "The host has left the session.",
            LocalError => "Could not connect - check Steam is running and try again.",
            _ => string.IsNullOrEmpty(reason) ? "Disconnected." : reason
        };
    }
}
