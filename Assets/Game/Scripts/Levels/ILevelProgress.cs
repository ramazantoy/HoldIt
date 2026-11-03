namespace HoldIt.Levels
{
    /// <summary>
    /// How far the player got: which levels are open and the best stars won in each. Saved with the global data, so it
    /// follows the player to other devices. Levels are counted by their place in the level list, from 0.
    /// </summary>
    public interface ILevelProgress
    {
        /// <summary>How many levels are open, from the first: at least 1, at most the level count.</summary>
        int Unlocked { get; }

        bool IsUnlocked(int index);

        /// <summary>The most stars won in the level; 0 until it is won.</summary>
        int BestStars(int index);

        /// <summary>The level is won with <paramref name="stars"/>: its best stars can only rise, and the next level opens.</summary>
        void RecordWin(int index, int stars);
    }
}
