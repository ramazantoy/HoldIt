namespace HoldIt.Levels
{
    public enum LevelOutcome
    {
        Playing,

        /// <summary>Everyone came and went before the lives ran out.</summary>
        Won,

        /// <summary>The lives ran out.</summary>
        Lost,
    }
}
