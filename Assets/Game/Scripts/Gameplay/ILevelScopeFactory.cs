using HoldIt.Levels;

namespace HoldIt.Gameplay
{
    /// <summary>Builds a level's scope, below the gameplay scene's.</summary>
    internal interface ILevelScopeFactory
    {
        /// <summary>
        /// Builds everything <paramref name="level"/> needs (its board, restrooms, characters, run, spawning, drawing and
        /// flows). Disposing the result takes all of it down again, subscriptions, pools and pieces included.
        /// </summary>
        ILevel Create(LevelDefinition level);
    }
}
