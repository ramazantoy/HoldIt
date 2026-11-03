namespace HoldIt.Characters
{
    /// <summary>Why a character gave up a step, as its speech bubble says it.</summary>
    public enum StepRefusal
    {
        /// <summary>Nothing was refused.</summary>
        None,

        /// <summary>It reached the other gender's door and walks back.</summary>
        WrongDoor,

        /// <summary>It is Fat, reached a narrow passage and walks back.</summary>
        TooWide,

        /// <summary>Someone stood in its way for too long; it stops where it is.</summary>
        Blocked,
    }
}
