using System;

namespace HoldIt.Gameplay
{
    /// <summary>The pause screen: back to the game, or to the menu.</summary>
    public interface IPauseView
    {
        event Action ResumeClicked;
        event Action MenuClicked;

        void Show();
        void Hide();
    }
}
