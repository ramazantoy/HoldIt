using UnityEngine;

namespace HoldIt.Backdrop
{
    /// <summary>The two colours of the backdrop's vertical gradient.</summary>
    public readonly struct BackdropColors
    {
        public readonly Color Top;
        public readonly Color Bottom;

        public BackdropColors(Color top, Color bottom)
        {
            Top = top;
            Bottom = bottom;
        }

        public static BackdropColors Lerp(BackdropColors from, BackdropColors to, float t) =>
            new BackdropColors(Color.Lerp(from.Top, to.Top, t), Color.Lerp(from.Bottom, to.Bottom, t));

        /// <summary>How far apart two gradients are: the mean RGB distance of their tops and of their bottoms.</summary>
        public static float Distance(BackdropColors a, BackdropColors b) =>
            (Between(a.Top, b.Top) + Between(a.Bottom, b.Bottom)) * 0.5f;

        private static float Between(Color a, Color b) =>
            new Vector3(a.r - b.r, a.g - b.g, a.b - b.b).magnitude;
    }
}
