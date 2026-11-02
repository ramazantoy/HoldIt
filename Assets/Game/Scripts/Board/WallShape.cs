namespace HoldIt.Board
{
    /// <summary>
    /// The six wall block models (<see cref="WallAutotile"/>). Each joins the blocks on some of its sides seamlessly and has
    /// finished faces on the others. The sides it joins when facing North (yaw 0) are listed below; turning it clockwise
    /// turns them with it.
    /// </summary>
    public enum WallShape
    {
        /// <summary>Joins nothing.</summary>
        Single,

        /// <summary>Joins north.</summary>
        End,

        /// <summary>Joins north and south.</summary>
        Straight,

        /// <summary>Joins north and east.</summary>
        Corner,

        /// <summary>Joins north, east and south.</summary>
        T,

        /// <summary>Joins all four sides.</summary>
        Cross,
    }
}
