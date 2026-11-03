using System;

namespace HoldIt.Gameplay
{
    /// <summary>The gameplay HUD: the level, the lives, who made it, and the pause button.</summary>
    public interface IGameplayHudView
    {
        event Action PauseClicked;

        /// <param name="number">From 1.</param>
        void ShowLevel(int number);

        void ShowServed(int served, int crowd);
        void ShowLives(int lives, int maxLives);
    }
}
