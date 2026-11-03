namespace HoldIt.Characters
{
    /// <summary>Why a character left the board.</summary>
    public enum RemovalReason
    {
        /// <summary>It walked all the way into its stall, which shuts behind it.</summary>
        EnteredStall,

        /// <summary>Its patience ran out before it got there: an accident, where it stood.</summary>
        Accident,
    }
}
