using System;

namespace Game.Core
{
    // GUID for IP connections, SteamID64 string for Steam
    public readonly struct PlayerId : IEquatable<PlayerId>
    {
        public string Value { get; }

        public PlayerId(string value) => Value = value ?? string.Empty;

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public bool Equals(PlayerId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is PlayerId other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public override string ToString() => Value;

        public static bool operator ==(PlayerId a, PlayerId b) => a.Equals(b);
        public static bool operator !=(PlayerId a, PlayerId b) => !a.Equals(b);

        public static readonly PlayerId None = new PlayerId(string.Empty);
    }
}
