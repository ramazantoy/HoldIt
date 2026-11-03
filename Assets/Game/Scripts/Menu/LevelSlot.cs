namespace HoldIt.Menu
{
    /// <summary>One level as the level select shows it.</summary>
    public readonly struct LevelSlot
    {
        /// <summary>From 1.</summary>
        public readonly int Number;

        /// <summary>The best stars won; 0 until the level is won.</summary>
        public readonly int Stars;

        public readonly bool IsOpen;

        /// <summary>The last open level: where the player got to.</summary>
        public readonly bool IsNewest;

        public LevelSlot(int number, int stars, bool isOpen, bool isNewest)
        {
            Number = number;
            Stars = stars;
            IsOpen = isOpen;
            IsNewest = isNewest;
        }
    }
}
