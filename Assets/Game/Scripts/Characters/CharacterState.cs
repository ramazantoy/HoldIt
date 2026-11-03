namespace HoldIt.Characters
{
    /// <summary>
    /// What a character is doing. An accident is no state: the character leaves the board there and then
    /// (<see cref="RemovalReason.Accident"/>).
    /// </summary>
    public enum CharacterState
    {
        /// <summary>Standing on its cell; a path can be given to it.</summary>
        Idle,

        /// <summary>Following its path one cell at a time; every step is checked before it is taken.</summary>
        Walking,

        /// <summary>At its restroom's door with every stall busy; it holds the door until a stall frees.</summary>
        Queued,

        /// <summary>Walking from the door into the stall it took; once in, it leaves the board.</summary>
        InStall,
    }
}
