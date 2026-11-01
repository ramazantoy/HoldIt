using Cysharp.Threading.Tasks;

namespace AF.Core
{
    public interface IGameManager
    {
        IGameState Current { get; }
        bool IsTransitioning { get; }

        /// <summary>
        /// Exits the current state and enters <typeparamref name="TState"/>, then publishes <see cref="GameStateChanged"/>.
        /// Going to the current state does nothing. Fails with <see cref="System.InvalidOperationException"/> when the
        /// state is not registered or another transition is still running.
        /// </summary>
        /// <remarks>
        /// If the current state's Exit throws, the game stays in it and nothing is published. If the next state's Enter throws,
        /// the game is in the next state: <see cref="GameStateChanged"/> is still published, then the exception reaches the caller.
        /// </remarks>
        UniTask GoTo<TState>() where TState : IGameState;
    }
}
