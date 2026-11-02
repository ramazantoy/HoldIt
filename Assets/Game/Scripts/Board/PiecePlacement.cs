namespace HoldIt.Board
{
    /// <summary>
    /// One piece of the board: what it is, whose (gendered pieces only), its shape (wall blocks only), on which tile, and
    /// which edge it faces.
    /// </summary>
    public readonly struct PiecePlacement
    {
        public readonly PieceKind Kind;
        public readonly Gender? Gender;
        public readonly WallShape? Shape;
        public readonly Cell Cell;
        public readonly Direction Facing;

        public PiecePlacement(PieceKind kind, Cell cell, Direction facing = Direction.North, Gender? gender = null,
            WallShape? shape = null)
        {
            Kind = kind;
            Cell = cell;
            Facing = facing;
            Gender = gender;
            Shape = shape;
        }

        public override string ToString()
        {
            var kind = Shape.HasValue ? $"{Kind} {Shape}" : Kind.ToString();
            return Gender.HasValue ? $"{Gender} {kind} {Cell} {Facing}" : $"{kind} {Cell} {Facing}";
        }
    }
}
