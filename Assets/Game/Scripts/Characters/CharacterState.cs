namespace HoldIt.Characters
{
    /// <summary>
    /// What a character is doing. Returning (M3), Queued and InStall (M4) and Accident (M6) join with their milestones.
    /// </summary>
    public enum CharacterState
    {
        /// <summary>Standing on its cell; a path can be given to it.</summary>
        Idle,

        /// <summary>Following its path one cell at a time.</summary>
        Walking,
    }
}
