using System.Collections.Generic;
using HoldIt.Levels;

namespace HoldIt.Menu
{
    /// <summary>The level select's cells, from the progress.</summary>
    internal static class LevelSlots
    {
        public static IReadOnlyList<LevelSlot> Build(ILevelProgress progress, int count)
        {
            var slots = new LevelSlot[count];
            var newest = progress.Unlocked - 1;
            for (var i = 0; i < count; i++)
                slots[i] = new LevelSlot(i + 1, progress.BestStars(i), progress.IsUnlocked(i), i == newest);
            return slots;
        }
    }
}
