using System;

namespace HoldIt.Gameplay
{
    /// <summary>The gameplay HUD: the level, the lives, who made it, and the pause button.</summary>
    public interface IGameplayHudView
    {
        event Action PauseClicked;

        /// <param name="number">From 1.</param>
        void ShowLevel(int number);

        /// <param name="animate">True when the number just changed: the pill swells. False for the first reading.</param>
        void ShowServed(int served, int crowd, bool animate);

        /// <param name="animate">True when a life was just lost: the screen flashes, the pill swells, the heart greys.</param>
        void ShowLives(int lives, int maxLives, bool animate);

        /// <summary>The pause screen is up: the HUD is hidden and cannot be pressed. False brings it back.</summary>
        void SetPaused(bool paused);
    }
}
