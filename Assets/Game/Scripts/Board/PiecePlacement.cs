namespace HoldIt.Board
{
    /// <summary>One piece of the board: what it is, whose (gendered pieces only), on which tile, and which edge it faces.</summary>
    public readonly struct PiecePlacement
    {
        public readonly PieceKind Kind;
        public readonly Gender? Gender;
        public readonly Cell Cell;
        public readonly Direction Facing;

        public PiecePlacement(PieceKind kind, Cell cell, Direction facing = Direction.North, Gender? gender = null)
        {
            Kind = kind;
            Cell = cell;
            Facing = facing;
            Gender = gender;
        }

        public override string ToString() =>
            Gender.HasValue ? $"{Gender} {Kind} {Cell} {Facing}" : $"{Kind} {Cell} {Facing}";
    }
}
