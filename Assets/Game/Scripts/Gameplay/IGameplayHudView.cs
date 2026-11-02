using System;

namespace HoldIt.Gameplay
{
    /// <summary>The gameplay HUD. For now only a button back to the menu.</summary>
    public interface IGameplayHudView
    {
        event Action MenuClicked;
    }
}
