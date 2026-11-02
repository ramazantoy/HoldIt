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

        /// <summary>A wall tile; it covers its own floor. Shaped by the wall blocks around it (<see cref="WallShape"/>).</summary>
        WallBlock,

        /// <summary>The posts of a narrow passage, standing on a floor piece. Faces North when the passage runs north-south.</summary>
        NarrowPosts,

        /// <summary>Floor inside a restroom. Gendered.</summary>
        BuildingFloor,

        /// <summary>A restroom's outer wall along one edge. Gendered.</summary>
        BuildingWall,

        /// <summary>
        /// The post on a restroom's outer corner where two of its walls meet. Gendered. Facing North it stands in the tile's
        /// north-east corner; East, South and West turn it to the south-east, south-west and north-west.
        /// </summary>
        BuildingCorner,

        /// <summary>A door tile: its own floor and the frame on the edge it opens to. Gendered.</summary>
        Doorway,
    }
}
