using AF.Core;
using AF.Input;

namespace Game
{
    /// <summary>
    /// The game's input modes: which action maps of Controls.inputactions are enabled together. Add a mode here and request it
    /// from IInputModeService; give every game state the game adds a mode in CreateOptions.
    /// </summary>
    public static class InputModes
    {
        public static readonly InputMode Menu = new InputMode("menu", CursorState.Free, "UI");

        /// <summary>The UI map stays on so HUD buttons and touch work; drop it if the HUD takes no input.</summary>
        public static readonly InputMode Gameplay = new InputMode("gameplay", CursorState.Free, "Gameplay", "UI");

        public static InputOptions CreateOptions() =>
            new InputOptions { Paused = Menu }
                .ForState<MenuState>(Menu)
                .ForState<PlayingState>(Gameplay);
    }
}
