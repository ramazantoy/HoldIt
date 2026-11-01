using System;
using System.Collections.Generic;

namespace AF.Platform
{
    /// <summary>Passed to <c>builder.UsePlatform(messagePipe, options)</c> in the root scope.</summary>
    public sealed class PlatformOptions
    {
        /// <summary>
        /// Every achievement id the game unlocks, exactly as defined in the store's back office. Unlocking any other id is
        /// logged as an error, so a typo shows up in the Editor instead of being ignored by the store.
        /// </summary>
        public IReadOnlyCollection<string> AchievementIds { get; set; } = Array.Empty<string>();

        /// <summary>Pause the game while the store overlay (Steam, Epic) is open.</summary>
        public bool PauseOnOverlay { get; set; } = true;
    }
}
