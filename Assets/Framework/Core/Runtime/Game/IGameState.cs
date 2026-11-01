using System.Threading;
using Cysharp.Threading.Tasks;

namespace AF.Core
{
    /// <summary>
    /// One state of the game flow. Register each state with <c>builder.Register&lt;T&gt;(Lifetime.Singleton).As&lt;IGameState&gt;()</c>;
    /// <see cref="IGameManager"/> looks states up by their concrete type.
    /// </summary>
    public interface IGameState
    {
        UniTask Enter(CancellationToken cancellation);
        UniTask Exit(CancellationToken cancellation);
    }

    /// <summary>Base class with empty enter/exit hooks.</summary>
    public abstract class GameState : IGameState
    {
        public virtual UniTask Enter(CancellationToken cancellation) => UniTask.CompletedTask;
        public virtual UniTask Exit(CancellationToken cancellation) => UniTask.CompletedTask;
        public override string ToString() => GetType().Name;
    }

    /// <summary>Initial state while the app boots. The first scene's <see cref="SceneScope"/> leaves it.</summary>
    public sealed class BootState : GameState
    {
    }

    public sealed class MenuState : GameState
    {
    }

    /// <summary>Gameplay. Pause is not a state; see <see cref="IPauseService"/>.</summary>
    public sealed class PlayingState : GameState
    {
    }
}
