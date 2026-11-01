namespace AF.Core
{
    /// <summary>
    /// Pause-aware game time. Compare against <see cref="Time"/> instead of caching <c>UnityEngine.Time.time</c>.
    /// The clock is the only writer of <c>UnityEngine.Time.timeScale</c>: paused means 0, otherwise <see cref="Scale"/>.
    /// </summary>
    public interface IGameClock
    {
        /// <summary>Seconds of game time since the root scope started. Does not advance while paused.</summary>
        float Time { get; }

        /// <summary>Game time elapsed this frame; 0 while paused.</summary>
        float DeltaTime { get; }

        /// <summary>Gameplay speed (slow motion, fast forward). Kept while paused and restored on resume.</summary>
        float Scale { get; set; }
    }
}
