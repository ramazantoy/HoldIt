using System;
using System.Collections.Generic;

namespace HoldIt.Backdrop
{
    internal sealed class BackdropMemory : IBackdropMemory
    {
        private readonly Random _random;
        private readonly List<int> _bag = new List<int>();
        private int _level = -1;

        public BackdropMemory(Random random)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public int Current { get; private set; } = -1;

        public int Choose(int levelIndex, int paletteCount)
        {
            if (levelIndex == _level && Current >= 0 && Current < paletteCount)
                return Current;

            Current = _bag.Draw(paletteCount, Current, _random);
            _level = levelIndex;
            return Current;
        }
    }
}
