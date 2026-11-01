using System.Collections.Generic;
using UnityEngine;

namespace AF.Settings
{
    /// <summary>
    /// The player's display settings, saved with this device's data and applied at startup. The only writer of
    /// <c>Screen.SetResolution</c>, the fullscreen mode, the quality level, <c>QualitySettings.vSyncCount</c>,
    /// <c>Application.targetFrameRate</c> and <c>IInputModeService.ConfineCursor</c>. Setters apply at once and publish
    /// <see cref="DisplaySettingsChanged"/>; they never write to disk (see Settings.md).
    /// </summary>
    public interface IDisplaySettings
    {
        DisplayState Current { get; }

        /// <summary>False on mobile: there is no window, so resolution, window mode, vsync and cursor confinement do not apply
        /// and their setters throw.</summary>
        bool HasWindow { get; }

        /// <summary>The window modes this platform offers; empty on mobile.</summary>
        IReadOnlyList<WindowMode> WindowModes { get; }

        /// <summary>
        /// The sizes offered for the current window mode, largest first, each once whatever its refresh rates: up to the
        /// display's own size, and in a window only sizes that fit beside the taskbar with a title bar. Read it again after
        /// <see cref="DisplaySettingsChanged"/>.
        /// </summary>
        IReadOnlyList<Vector2Int> Resolutions { get; }

        /// <summary>The quality levels this platform builds with, as named in Project Settings > Quality.</summary>
        IReadOnlyList<string> QualityLevels { get; }

        /// <summary>Throws for a size not in <see cref="Resolutions"/>.</summary>
        void SetResolution(Vector2Int size);

        /// <summary>Throws for a mode not in <see cref="WindowModes"/>.</summary>
        void SetWindowMode(WindowMode mode);

        void SetVSync(bool on);

        /// <summary>Frames per second, or 0 for no limit. Throws below 15. Ignored while vsync is on.</summary>
        void SetFrameRateLimit(int framesPerSecond);

        /// <summary>Throws for a name not in <see cref="QualityLevels"/>.</summary>
        void SetQuality(string level);

        void SetConfineCursor(bool on);

        /// <summary>Clears every choice: the defaults apply, including those of later builds.</summary>
        void ResetToDefaults();
    }
}
