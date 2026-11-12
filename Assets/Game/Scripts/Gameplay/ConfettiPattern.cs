using System;
using UnityEngine;

namespace HoldIt.Gameplay
{
    /// <summary>One piece of confetti: how it is thrown (canvas units per second), how it turns and when it goes off.</summary>
    public readonly struct ConfettiPiece
    {
        public ConfettiPiece(Vector2 velocity, float spin, int color, float delay)
        {
            Velocity = velocity;
            Spin = spin;
            Color = color;
            Delay = delay;
        }

        public Vector2 Velocity { get; }

        /// <summary>Degrees per second.</summary>
        public float Spin { get; }

        /// <summary>An index into the style's colours.</summary>
        public int Color { get; }

        /// <summary>Seconds after the burst starts that this piece leaves.</summary>
        public float Delay { get; }
    }

    /// <summary>
    /// The confetti of a won level's result screen, worked out without a scene: a seeded throw (so a test and a replay
    /// agree) and where a piece is at a time (a throw under gravity). The view only places its images from it.
    /// </summary>
    public static class ConfettiPattern
    {
        /// <summary>
        /// Throws <paramref name="count"/> pieces upwards inside a fan of <paramref name="spreadDegrees"/>, each with a speed
        /// between the two, a spin up to <paramref name="maxSpin"/> either way and a start up to <paramref name="maxDelay"/>
        /// late. Colours go round, so every one is used evenly.
        /// </summary>
        public static ConfettiPiece[] Make(int count, int seed, float spreadDegrees, float minSpeed, float maxSpeed,
            float maxSpin, int colors, float maxDelay)
        {
            var random = new System.Random(seed);
            var pieces = new ConfettiPiece[Math.Max(count, 0)];
            for (var i = 0; i < pieces.Length; i++)
            {
                var angle = (90f + ((float)random.NextDouble() - 0.5f) * spreadDegrees) * Mathf.Deg2Rad;
                var speed = Mathf.Lerp(minSpeed, maxSpeed, (float)random.NextDouble());
                var spin = ((float)random.NextDouble() * 2f - 1f) * maxSpin;
                var delay = (float)random.NextDouble() * maxDelay;
                pieces[i] = new ConfettiPiece(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * speed, spin,
                    colors > 0 ? i % colors : 0, delay);
            }
            return pieces;
        }

        /// <summary>
        /// Where the piece is, from where it was thrown, <paramref name="time"/> seconds into the burst. A negative
        /// <paramref name="gravity"/> pulls it down. Before its delay it is still at the start.
        /// </summary>
        public static Vector2 PositionAt(ConfettiPiece piece, float time, float gravity)
        {
            var t = Mathf.Max(time - piece.Delay, 0f);
            return piece.Velocity * t + new Vector2(0f, 0.5f * gravity * t * t);
        }

        /// <summary>The turn, in degrees.</summary>
        public static float AngleAt(ConfettiPiece piece, float time) => piece.Spin * Mathf.Max(time - piece.Delay, 0f);

        /// <summary>0 before the piece leaves, 1 while it flies, falling to 0 over the last <paramref name="fadeSeconds"/> of the burst.</summary>
        public static float AlphaAt(ConfettiPiece piece, float time, float seconds, float fadeSeconds)
        {
            if (time < piece.Delay)
                return 0f;
            return fadeSeconds <= 0f ? (time < seconds ? 1f : 0f) : Mathf.Clamp01((seconds - time) / fadeSeconds);
        }
    }
}
