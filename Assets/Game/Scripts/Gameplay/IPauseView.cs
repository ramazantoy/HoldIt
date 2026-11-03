using System;

namespace HoldIt.Gameplay
{
    /// <summary>The pause screen: back to the game, the settings, or the menu.</summary>
    public interface IPauseView
    {
        event Action ResumeClicked;
        event Action SettingsClicked;
        event Action MenuClicked;

        void Show();
        void Hide();
    }
}
