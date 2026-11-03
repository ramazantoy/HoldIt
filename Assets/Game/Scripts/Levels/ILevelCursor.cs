namespace HoldIt.Levels
{
    /// <summary>
    /// Which level is played: the gameplay scene builds the current one. It lives in the root scope, so it outlasts the
    /// scene. It is on the first level when the game starts; the menu selects the level to play. Only an open level
    /// (<see cref="ILevelProgress"/>) can be selected or moved on to.
    /// </summary>
    public interface ILevelCursor
    {
        /// <summary>From 0.</summary>
        int Index { get; }

        /// <summary>From 1, as the player reads it.</summary>
        int Number { get; }

        int Count { get; }
        LevelDefinition Current { get; }

        /// <summary>Whether a level follows this one, open or not.</summary>
        bool HasNext { get; }

        /// <summary>Whether a level follows this one and it is open.</summary>
        bool IsNextOpen { get; }

        /// <summary>Moves to the level at <paramref name="index"/>. Throws for an index out of range or a locked level.</summary>
        void Select(int index);

        /// <summary>Moves on to the next level. Throws on the last one and when the next one is locked.</summary>
        void Next();
    }
}
