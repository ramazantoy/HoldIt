namespace HoldIt.Board
{
    /// <summary>
    /// The board's building blocks (<see cref="BoardLayout"/>), one prefab each in the <see cref="TileSet"/>. Every piece is
    /// modelled around its tile's centre on the floor; edge pieces sit on the tile's north edge and are turned to the others.
    /// </summary>
    public enum PieceKind
    {
        Floor,
        LobbyFloor,
        EntranceFloor,

        /// <summary>A wall tile; it covers its own floor.</summary>
        WallBlock,

        /// <summary>The posts of a narrow passage, standing on a floor piece. Faces North when the passage runs north-south.</summary>
        NarrowPosts,

        /// <summary>Floor inside a restroom. Gendered.</summary>
        BuildingFloor,

        /// <summary>A restroom's outer wall along one edge. Gendered.</summary>
        BuildingWall,

        /// <summary>A door tile: its own floor and the frame on the edge it opens to. Gendered.</summary>
        Doorway,
    }
}
