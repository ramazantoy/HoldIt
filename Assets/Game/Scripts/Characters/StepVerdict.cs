namespace HoldIt.Characters
{
    /// <summary>What happens to a step a character is about to take.</summary>
    public enum StepVerdict
    {
        /// <summary>The character steps into the cell, which is now held for it.</summary>
        Enter,

        /// <summary>Someone else holds the cell: the character waits at its edge.</summary>
        Wait,

        /// <summary>The cell is the other gender's door: the character turns back.</summary>
        WrongDoor,

        /// <summary>The cell is a narrow passage and the character is Fat: it turns back.</summary>
        TooWide,
    }
}
