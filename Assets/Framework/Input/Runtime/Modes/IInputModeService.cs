using System;
using System.Collections.Generic;

namespace AF.Input
{
    /// <summary>
    /// The only owner of the action maps' enabled state and of the cursor. Code never enables or disables a map or an action
    /// itself; it requests the mode it needs and disposes the handle when done, like a pause request.
    /// </summary>
    public interface IInputModeService
    {
        /// <summary>The mode whose maps are enabled now.</summary>
        InputMode Current { get; }

        /// <summary>Live requests, oldest first, the pause included while paused. Meant for debugging input that does not respond.</summary>
        IReadOnlyList<InputMode> Requests { get; }

        /// <summary>
        /// Makes <paramref name="mode"/> active until the handle is disposed. The newest live request wins; with none alive, the
        /// current game state's mode applies. Disposing a handle removes only its own request, in any order, and disposing
        /// twice is harmless. Throws when the mode lists a map the actions asset does not have.
        /// </summary>
        IDisposable Request(InputMode mode);

        /// <summary>
        /// Keeps the cursor inside the game window while the mode's cursor is free or hidden (<c>CursorLockMode.Confined</c>);
        /// a locked cursor is unaffected. Takes effect on Windows and Linux. The Settings module sets and saves it.
        /// </summary>
        bool ConfineCursor { get; set; }
    }
}
