using System;

namespace HoldIt.Characters
{
    /// <summary>Names one character for as long as it is on the board. The default value names nobody.</summary>
    public readonly struct CharacterId : IEquatable<CharacterId>
    {
        public readonly int Value;

        public CharacterId(int value) => Value = value;

        public bool Equals(CharacterId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is CharacterId other && Equals(other);
        public override int GetHashCode() => Value;
        public static bool operator ==(CharacterId a, CharacterId b) => a.Equals(b);
        public static bool operator !=(CharacterId a, CharacterId b) => !a.Equals(b);

        public override string ToString() => $"#{Value}";
    }
}
