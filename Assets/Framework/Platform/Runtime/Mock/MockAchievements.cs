using System;
using System.Collections.Generic;
using AF.Core;

namespace AF.Platform
{
    /// <summary>Achievements kept in memory for the session. Checks ids like a store would, but loudly.</summary>
    public sealed class MockAchievements : IAchievements
    {
        private readonly HashSet<string> _known;
        private readonly HashSet<string> _unlocked = new HashSet<string>(StringComparer.Ordinal);

        public MockAchievements(PlatformOptions options) =>
            _known = new HashSet<string>(options.AchievementIds ?? Array.Empty<string>(), StringComparer.Ordinal);

        public IReadOnlyCollection<string> Unlocked => _unlocked;

        public void Unlock(string id)
        {
            if (id == null || !_known.Contains(id))
            {
                Log.Error($"Platform: unknown achievement id '{id}'. Add it to PlatformOptions.AchievementIds; a store ignores ids it does not define.");
                return;
            }
            if (_unlocked.Add(id))
                Log.Info($"Platform (mock): achievement '{id}' unlocked");
        }

        public bool IsUnlocked(string id) => id != null && _unlocked.Contains(id);

        /// <summary>Locks every achievement again, like a store's developer reset.</summary>
        public void ResetAll() => _unlocked.Clear();
    }
}
