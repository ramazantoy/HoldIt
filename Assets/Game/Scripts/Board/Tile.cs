using System;

namespace HoldIt.Board
{
    /// <summary>One map tile: its type and, for buildings and doors, whose it is.</summary>
    public readonly struct Tile : IEquatable<Tile>
    {
        public readonly TileType Type;

        /// <summary>Set for <see cref="TileType.Building"/> and <see cref="TileType.Door"/>, null otherwise.</summary>
        public readonly Gender? Gender;

        public Tile(TileType type, Gender? gender = null)
        {
            Type = type;
            Gender = gender;
        }

        public bool IsWalkable => Type != TileType.Wall && Type != TileType.Building;

        public bool Equals(Tile other) => Type == other.Type && Gender == other.Gender;
        public override bool Equals(object obj) => obj is Tile other && Equals(other);
        public override int GetHashCode() => ((int)Type * 397) ^ Gender.GetHashCode();
        public static bool operator ==(Tile a, Tile b) => a.Equals(b);
        public static bool operator !=(Tile a, Tile b) => !a.Equals(b);

        public override string ToString() => Gender.HasValue ? $"{Gender} {Type}" : Type.ToString();
    }
}
