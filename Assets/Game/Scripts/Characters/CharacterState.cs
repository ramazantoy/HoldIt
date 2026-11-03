namespace HoldIt.Characters
{
    /// <summary>What a character is doing. Queued and InStall (M4) and Accident (M6) join with their milestones.</summary>
    public enum CharacterState
    {
        /// <summary>Standing on its cell; a path can be given to it.</summary>
        Idle,

        /// <summary>Following its path one cell at a time; every step is checked before it is taken.</summary>
        Walking,

        /// <summary>Refused on the way, walking back to where its path began; only other characters can stop it.</summary>
        Returning,
    }
}
