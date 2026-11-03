namespace HoldIt.Characters
{
    /// <summary>Why a character gave up a step, as its speech bubble says it.</summary>
    public enum StepRefusal
    {
        /// <summary>Nothing was refused.</summary>
        None,

        /// <summary>It reached the other gender's door and stops in front of it.</summary>
        WrongDoor,

        /// <summary>It is Fat, reached a narrow passage and stops in front of it.</summary>
        TooWide,

        /// <summary>Someone stood in its way for too long; it stops where it is.</summary>
        Blocked,
    }
}
