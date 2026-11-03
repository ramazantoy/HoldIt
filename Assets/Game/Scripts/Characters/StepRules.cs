using HoldIt.Board;

namespace HoldIt.Characters
{
    /// <summary>The tile rules a walking character meets on each step. Who else stands there is the claims' business.</summary>
    public static class StepRules
    {
        /// <summary>
        /// <see cref="StepVerdict.WrongDoor"/> for the other gender's door, <see cref="StepVerdict.TooWide"/> for a narrow
        /// passage when Fat, otherwise <see cref="StepVerdict.Enter"/>.
        /// </summary>
        public static StepVerdict Check(Tile tile, Gender gender, BodySize size)
        {
            if (tile.Type == TileType.Door && tile.Gender != gender)
                return StepVerdict.WrongDoor;
            if (tile.Type == TileType.Narrow && size == BodySize.Fat)
                return StepVerdict.TooWide;
            return StepVerdict.Enter;
        }
    }
}
