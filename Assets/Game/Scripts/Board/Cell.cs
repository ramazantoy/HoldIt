using System;

namespace HoldIt.Board
{
    /// <summary>A tile position: column from the left, row from the top. Row 0 is the row farthest from the camera.</summary>
    public readonly struct Cell : IEquatable<Cell>
    {
        public readonly int Col;
        public readonly int Row;

        public Cell(int col, int row)
        {
            Col = col;
            Row = row;
        }

        /// <summary>The neighbouring cell on that side. It may be outside the map.</summary>
        public Cell Step(Direction direction) => direction switch
        {
            Direction.North => new Cell(Col, Row - 1),
            Direction.East => new Cell(Col + 1, Row),
            Direction.South => new Cell(Col, Row + 1),
            _ => new Cell(Col - 1, Row),
        };

        /// <summary>True when the other cell shares a side with this one (not a corner, not the cell itself).</summary>
        public bool IsNeighbour(Cell other) => Math.Abs(Col - other.Col) + Math.Abs(Row - other.Row) == 1;

        /// <summary>The side of this cell the neighbour is on. False when <paramref name="neighbour"/> is not a neighbour.</summary>
        public bool TryDirectionTo(Cell neighbour, out Direction direction)
        {
            foreach (var candidate in Directions.All)
            {
                if (Step(candidate) == neighbour)
                {
                    direction = candidate;
                    return true;
                }
            }

            direction = default;
            return false;
        }

        public bool Equals(Cell other) => Col == other.Col && Row == other.Row;
        public override bool Equals(object obj) => obj is Cell other && Equals(other);
        public override int GetHashCode() => (Col * 397) ^ Row;
        public static bool operator ==(Cell a, Cell b) => a.Equals(b);
        public static bool operator !=(Cell a, Cell b) => !a.Equals(b);

        public override string ToString() => $"({Col}, {Row})";
    }
}
