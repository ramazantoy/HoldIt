namespace HoldIt.Levels
{
    /// <summary>
    /// The level being played: who made it, the lives left and how it ended. Its changes come as <see cref="ServedChanged"/>,
    /// <see cref="LivesChanged"/> and <see cref="LevelEnded"/>, each after the run has changed.
    /// </summary>
    public interface ILevelRun
    {
        /// <summary>How many come into the level: the whole the score is out of.</summary>
        int Crowd { get; }

        /// <summary>How many made it into a stall: the score.</summary>
        int Served { get; }

        int Lives { get; }
        int MaxLives { get; }
        LevelOutcome Outcome { get; }

        /// <summary>The lives left once won; 0 while playing and when lost.</summary>
        int Stars { get; }
    }
}
