using System.Collections.Generic;

namespace HoldIt.Levels
{
    /// <summary>The saved progress (<see cref="LevelProgress"/>). Levels are counted by their place in the level list.</summary>
    public sealed class LevelProgressData
    {
        /// <summary>Best stars per level, from the first; 0 or missing (a shorter list): not won yet.</summary>
        public List<int> stars = new List<int>();

        /// <summary>How many levels are open, from the first.</summary>
        public int unlocked = 1;
    }
}
