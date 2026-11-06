using System;
using HoldIt.Board;
using HoldIt.Characters;

namespace HoldIt.Drawing
{
    /// <summary>Which mark this is: one cell of one character's path. A drawn path and the walk that follows it share keys.</summary>
    internal readonly struct MarkerKey : IEquatable<MarkerKey>
    {
        public readonly CharacterId Owner;
        public readonly Cell Cell;

        public MarkerKey(CharacterId owner, Cell cell)
        {
            Owner = owner;
            Cell = cell;
        }

        public bool Equals(MarkerKey other) => Owner == other.Owner && Cell.Equals(other.Cell);
        public override bool Equals(object obj) => obj is MarkerKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Owner, Cell);
        public override string ToString() => $"{Owner}@{Cell}";
    }
}
