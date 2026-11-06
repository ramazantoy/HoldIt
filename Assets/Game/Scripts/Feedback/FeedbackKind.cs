namespace HoldIt.Feedback
{
    /// <summary>
    /// What happened, as far as the senses are concerned: each kind has its sound and vibration in the
    /// <see cref="FeedbackSet"/>. Game code names the event and never a clip.
    /// </summary>
    public enum FeedbackKind
    {
        /// <summary>A button was pressed.</summary>
        UiClick,

        /// <summary>The path being drawn grew by a cell.</summary>
        PathCell,

        /// <summary>The path being drawn reached a cell it cannot end on.</summary>
        PathBlocked,

        /// <summary>A drawn path was let go and the character set off.</summary>
        PathSent,

        /// <summary>A character gave up a step: the wrong door, or too wide.</summary>
        Refused,

        /// <summary>A character gave up a step because the cell stayed occupied.</summary>
        Blocked,

        /// <summary>A character waiting at a taken stall's door knocked.</summary>
        Knock,

        /// <summary>A visitor went in and the stall's door shut.</summary>
        StallShut,

        /// <summary>A visitor was relieved and left its stall.</summary>
        Relieved,

        /// <summary>A visitor got into a stall: one more served.</summary>
        Served,

        /// <summary>A character had an accident: a life was lost.</summary>
        Accident,

        /// <summary>A new character came in.</summary>
        Spawn,

        /// <summary>A star popped in on the result screen.</summary>
        StarPop,

        /// <summary>The level was won.</summary>
        LevelWon,

        /// <summary>The level was lost.</summary>
        LevelLost,
    }
}
