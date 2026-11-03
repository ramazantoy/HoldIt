namespace HoldIt.Levels
{
    /// <summary>An accident cost a life; <see cref="Lives"/> are left.</summary>
    public readonly struct LivesChanged
    {
        public readonly int Lives;

        public LivesChanged(int lives) => Lives = lives;
    }
}
