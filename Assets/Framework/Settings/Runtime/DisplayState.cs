using System;
using UnityEngine;

namespace AF.Settings
{
    /// <summary>The display settings in effect: what the player chose, or the default where the choice is unset or invalid here.</summary>
    public readonly struct DisplayState : IEquatable<DisplayState>
    {
        public readonly Vector2Int Resolution;
        public readonly WindowMode WindowMode;
        public readonly bool VSync;

        /// <summary>Frames per second; 0 is no limit (the display's refresh rate on mobile).</summary>
        public readonly int FrameRateLimit;

        /// <summary>The quality level's name, as in Project Settings > Quality.</summary>
        public readonly string Quality;

        public readonly bool ConfineCursor;

        public DisplayState(Vector2Int resolution, WindowMode windowMode, bool vSync, int frameRateLimit, string quality, bool confineCursor)
        {
            Resolution = resolution;
            WindowMode = windowMode;
            VSync = vSync;
            FrameRateLimit = frameRateLimit;
            Quality = quality;
            ConfineCursor = confineCursor;
        }

        public bool Equals(DisplayState other) =>
            Resolution == other.Resolution && WindowMode == other.WindowMode && VSync == other.VSync &&
            FrameRateLimit == other.FrameRateLimit && Quality == other.Quality && ConfineCursor == other.ConfineCursor;

        public override bool Equals(object obj) => obj is DisplayState other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Resolution, WindowMode, VSync, FrameRateLimit, Quality, ConfineCursor);

        public override string ToString() =>
            $"{WindowMode} {Resolution.x}x{Resolution.y}, vsync {(VSync ? "on" : "off")}, " +
            $"frame limit {(FrameRateLimit == 0 ? "none" : FrameRateLimit.ToString())}, quality {Quality}, " +
            $"confine {(ConfineCursor ? "on" : "off")}";
    }
}
