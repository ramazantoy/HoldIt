using HoldIt.Levels;

namespace HoldIt.Gameplay
{
    /// <summary>What the result screen shows of an ended level.</summary>
    public readonly struct LevelResult
    {
        public readonly LevelOutcome Outcome;

        /// <summary>From 1.</summary>
        public readonly int Level;

        public readonly int Stars;
        public readonly int MaxStars;
        public readonly int Served;
        public readonly int Crowd;

        /// <summary>Whether a level follows this one, open or not.</summary>
        public readonly bool HasNext;

        /// <summary>Whether a level follows this one and the progress has it open: Next takes the player there.</summary>
        public readonly bool NextOpen;

        public LevelResult(LevelOutcome outcome, int level, int stars, int maxStars, int served, int crowd, bool hasNext,
            bool nextOpen)
        {
            Outcome = outcome;
            Level = level;
            Stars = stars;
            MaxStars = maxStars;
            Served = served;
            Crowd = crowd;
            HasNext = hasNext;
            NextOpen = nextOpen;
        }

        public bool IsWon => Outcome == LevelOutcome.Won;

        /// <summary>The last level won: every level is done.</summary>
        public bool IsGameDone => IsWon && !HasNext;
    }
}
