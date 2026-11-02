namespace HoldIt.Board
{
    /// <summary>What a map tile is. Map files write each as one character (<see cref="MapParser"/>).</summary>
    public enum TileType
    {
        /// <summary>Open floor.</summary>
        Floor,

        /// <summary>Floor where new characters wait for a path.</summary>
        Lobby,

        /// <summary>Floor where characters come in.</summary>
        Entrance,

        /// <summary>Not walkable; paths cannot cross it.</summary>
        Wall,

        /// <summary>Walkable, but too narrow for Fat characters.</summary>
        Narrow,

        /// <summary>Inside a restroom (its stalls); not walkable. Has a gender.</summary>
        Building,

        /// <summary>A restroom's door; only its gender may enter. Has a gender.</summary>
        Door,
    }
}
