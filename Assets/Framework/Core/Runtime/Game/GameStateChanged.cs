namespace AF.Core
{
    /// <summary>
    /// Published after a transition has finished; <see cref="Current"/> is already entered (also when its Enter threw: the game
    /// is in that state anyway).
    /// </summary>
    public readonly struct GameStateChanged
    {
        public readonly IGameState Previous;
        public readonly IGameState Current;

        public GameStateChanged(IGameState previous, IGameState current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
