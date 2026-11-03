namespace HoldIt.Restrooms
{
    /// <summary>Where a stall is in its round: free, taken by a visitor on the way in, or shut with the visitor inside.</summary>
    public enum StallState
    {
        /// <summary>Nobody has it; its lamp is green and its door open.</summary>
        Free,

        /// <summary>A visitor took it and is walking in; its lamp is red and its door still open.</summary>
        Taken,

        /// <summary>The visitor is inside behind the closed door; it frees when its time is up.</summary>
        Shut,
    }
}
