using System;

namespace HoldIt.Levels
{
    internal sealed class LevelCursor : ILevelCursor
    {
        private readonly LevelList _levels;
        private readonly ILevelProgress _progress;

        public LevelCursor(LevelList levels, ILevelProgress progress)
        {
            if (levels == null)
                throw new ArgumentNullException(nameof(levels));
            if (levels.Levels.Count == 0)
                throw new ArgumentException($"The level list '{levels.name}' is empty.", nameof(levels));
            _levels = levels;
            _progress = progress ?? throw new ArgumentNullException(nameof(progress));
        }

        public int Index { get; private set; }
        public int Number => Index + 1;
        public int Count => _levels.Levels.Count;
        public LevelDefinition Current => _levels.Levels[Index];
        public bool HasNext => Index < Count - 1;
        public bool IsNextOpen => HasNext && _progress.IsUnlocked(Index + 1);

        public void Select(int index)
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index), index, $"There are {Count} levels.");
            if (!_progress.IsUnlocked(index))
                throw new InvalidOperationException($"Level {index + 1} is locked.");
            Index = index;
        }

        public void Next()
        {
            if (!HasNext)
                throw new InvalidOperationException($"Level {Number} is the last one.");
            Select(Index + 1);
        }
    }
}
