using AF.Core;

namespace HoldIt.Editor.Simulation
{
    /// <summary>The game clock of a simulated level: every frame is <see cref="DeltaTime"/> long, and nothing pauses it.</summary>
    internal sealed class SimClock : IGameClock
    {
        public SimClock(float deltaTime) => DeltaTime = deltaTime;

        public float Time { get; private set; }
        public float DeltaTime { get; }
        public float Scale { get; set; } = 1f;

        public void Advance() => Time += DeltaTime;
    }
}
