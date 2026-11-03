namespace HoldIt.Characters
{
    /// <summary>What a character is doing. Accident (M6) joins with its milestone.</summary>
    public enum CharacterState
    {
        /// <summary>Standing on its cell; a path can be given to it.</summary>
        Idle,

        /// <summary>Following its path one cell at a time; every step is checked before it is taken.</summary>
        Walking,

        /// <summary>Refused on the way, walking back to where its path began; only other characters can stop it.</summary>
        Returning,

        /// <summary>At its restroom's door with every stall busy; it holds the door until a stall frees.</summary>
        Queued,

        /// <summary>Walking from the door into the stall it took; once in, it leaves the board.</summary>
        InStall,
    }
}
