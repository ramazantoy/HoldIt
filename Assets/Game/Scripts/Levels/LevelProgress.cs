using System;
using System.Collections.Generic;
using AF.Save;

namespace HoldIt.Levels
{
    /// <summary>
    /// Keeps <see cref="ILevelProgress"/> in the global save data. Every win opens the next level. The saved values are
    /// kept as they are and only limited when read, so a level list that shrinks and grows again loses nothing.
    /// </summary>
    internal sealed class LevelProgress : Savable<LevelProgressData>, ILevelProgress
    {
        private readonly LevelList _levels;
        private LevelProgressData _data = new LevelProgressData();

        public LevelProgress(LevelList levels)
        {
            if (levels == null)
                throw new ArgumentNullException(nameof(levels));
            _levels = levels;
        }

        public override string Key => "game.progress";
        public override SaveDocument Document => SaveDocument.Global;

        public int Unlocked => Math.Clamp(_data.unlocked, 1, Math.Max(1, Count));

        private int Count => _levels.Levels.Count;

        public bool IsUnlocked(int index) => index >= 0 && index < Unlocked;

        public int BestStars(int index) => index >= 0 && index < _data.stars.Count ? _data.stars[index] : 0;

        public void RecordWin(int index, int stars)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index), index, $"There are {Count} levels.");
            if (stars < 1)
                throw new ArgumentOutOfRangeException(nameof(stars), stars, "A won level has at least one star.");

            while (_data.stars.Count <= index)
                _data.stars.Add(0);
            _data.stars[index] = Math.Max(_data.stars[index], stars);
            _data.unlocked = Math.Max(_data.unlocked, Math.Min(index + 2, Count));
        }

        /// <summary>Debug: opens every level (the Editor's HoldIt/Progress menu).</summary>
        internal void UnlockAll() => _data.unlocked = Math.Max(_data.unlocked, Count);

        /// <summary>Debug: back to a new game's progress (the Editor's HoldIt/Progress menu).</summary>
        internal void ResetProgress() => _data = new LevelProgressData();

        protected override LevelProgressData Capture() => _data;

        protected override void Restore(LevelProgressData data)
        {
            // A file with "stars": null replaces the field initializer with null.
            var stars = new List<int>();
            if (data.stars != null)
                foreach (var value in data.stars)
                    stars.Add(Math.Max(0, value));
            _data = new LevelProgressData { stars = stars, unlocked = data.unlocked };
        }
    }
}
