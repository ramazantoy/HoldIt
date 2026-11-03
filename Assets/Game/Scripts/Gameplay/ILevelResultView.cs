using System;

namespace HoldIt.Gameplay
{
    /// <summary>The screen at a level's end: how it went, then the next level (when won and one follows), again, or the menu.</summary>
    public interface ILevelResultView
    {
        event Action NextClicked;
        event Action RetryClicked;
        event Action MenuClicked;

        void Show(LevelResult result);
        void Hide();
    }
}
