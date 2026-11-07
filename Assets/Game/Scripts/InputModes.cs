using AF.Core;
using AF.Input;

namespace HoldIt
{
    /// <summary>
    /// The game's input modes: which action maps of Controls.inputactions are enabled together. Add a mode here and request it
    /// from IInputModeService; give every game state the game adds a mode in CreateOptions.
    /// </summary>
    public static class InputModes
    {
        public static readonly InputMode Menu = new InputMode("menu", CursorState.Free, "UI");

        /// <summary>The UI map stays on: the HUD buttons take touches, and UI/Cancel is the back button (BackPressed).</summary>
        public static readonly InputMode Gameplay = new InputMode("gameplay", CursorState.Free, "Gameplay", "UI");

        /// <summary>No map at all: asked for while one level is taken down and the next built, as a scene load would.</summary>
        public static readonly InputMode Blocked = new InputMode("blocked", CursorState.Free);

        public static InputOptions CreateOptions() =>
            new InputOptions { Paused = Menu }
                .ForState<MenuState>(Menu)
                .ForState<PlayingState>(Gameplay);
    }
}
