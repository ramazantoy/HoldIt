using System;

namespace HoldIt.Menu
{
    /// <summary>The main menu screen: Play, Levels, Settings and (on desktop) Quit.</summary>
    public interface IMainMenuView
    {
        event Action PlayClicked;
        event Action LevelsClicked;
        event Action SettingsClicked;
        event Action QuitClicked;

        void Show(bool quitVisible);
        void Hide();
    }
}
