namespace HoldIt.Levels
{
    /// <summary>
    /// Which level is played: the gameplay scene builds the current one. It lives in the root scope, so it outlasts the
    /// scene. It starts at the first level whenever the game starts; choosing a level and keeping how far the player got
    /// come with the level select.
    /// </summary>
    public interface ILevelCursor
    {
        /// <summary>From 0.</summary>
        int Index { get; }

        /// <summary>From 1, as the player reads it.</summary>
        int Number { get; }

        int Count { get; }
        LevelDefinition Current { get; }
        bool HasNext { get; }

        /// <summary>Moves on to the next level. Throws on the last one.</summary>
        void Next();
    }
}
