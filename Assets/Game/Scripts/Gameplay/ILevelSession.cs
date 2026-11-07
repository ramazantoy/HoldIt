using Cysharp.Threading.Tasks;

namespace HoldIt.Gameplay
{
    /// <summary>Plays the levels one after another in the gameplay scene, without loading it again.</summary>
    internal interface ILevelSession
    {
        /// <summary>True from a request to change level until the new level is up.</summary>
        bool IsChanging { get; }

        /// <summary>Takes the level down and plays it again. Does nothing while a change is running.</summary>
        UniTask Retry();

        /// <summary>Moves the cursor on and plays that level. Does nothing while a change is running, or when the next level is not open.</summary>
        UniTask Next();
    }
}
