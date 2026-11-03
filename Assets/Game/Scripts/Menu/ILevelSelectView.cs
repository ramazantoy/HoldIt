using System;
using System.Collections.Generic;

namespace HoldIt.Menu
{
    /// <summary>The level select screen: a cell per level, open or locked, with its best stars.</summary>
    public interface ILevelSelectView
    {
        /// <summary>A cell was tapped: its level's index, from 0.</summary>
        event Action<int> LevelClicked;

        event Action BackClicked;

        /// <param name="maxStars">The stars a level can be won with (the lives).</param>
        void Show(IReadOnlyList<LevelSlot> slots, int maxStars);

        void Hide();
    }
}
