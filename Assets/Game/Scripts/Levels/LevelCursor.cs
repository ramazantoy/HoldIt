using System;

namespace HoldIt.Levels
{
    internal sealed class LevelCursor : ILevelCursor
    {
        private readonly LevelList _levels;

        public LevelCursor(LevelList levels)
        {
            if (levels == null)
                throw new ArgumentNullException(nameof(levels));
            if (levels.Levels.Count == 0)
                throw new ArgumentException($"The level list '{levels.name}' is empty.", nameof(levels));
            _levels = levels;
        }

        public int Index { get; private set; }
        public int Number => Index + 1;
        public int Count => _levels.Levels.Count;
        public LevelDefinition Current => _levels.Levels[Index];
        public bool HasNext => Index < Count - 1;

        public void Next()
        {
            if (!HasNext)
                throw new InvalidOperationException($"Level {Number} is the last one.");
            Index++;
        }
    }
}
