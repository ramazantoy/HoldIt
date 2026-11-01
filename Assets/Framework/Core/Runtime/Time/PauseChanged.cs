namespace AF.Core
{
    /// <summary>Published when the game goes from running to paused or back; not on every pause request.</summary>
    public readonly struct PauseChanged
    {
        public readonly bool IsPaused;

        public PauseChanged(bool isPaused) => IsPaused = isPaused;
    }
}
