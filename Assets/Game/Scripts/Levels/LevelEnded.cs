namespace HoldIt.Levels
{
    /// <summary>The level is won or lost. Nothing counts after it.</summary>
    public readonly struct LevelEnded
    {
        public readonly LevelOutcome Outcome;

        /// <summary>1 to the lives a level starts with when won (the lives left), 0 when lost.</summary>
        public readonly int Stars;

        public readonly int Served;

        public LevelEnded(LevelOutcome outcome, int stars, int served)
        {
            Outcome = outcome;
            Stars = stars;
            Served = served;
        }
    }
}
