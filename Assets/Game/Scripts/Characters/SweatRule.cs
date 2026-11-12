namespace HoldIt.Characters
{
    /// <summary>
    /// When a waiting character sweats: once its patience used up reaches a threshold, and until it falls back a margin
    /// under it. The margin keeps the drops from blinking on and off if the value hovers at the edge.
    /// </summary>
    public static class SweatRule
    {
        public static bool Next(bool sweating, float patience, float from, float margin) =>
            patience >= (sweating ? from - margin : from);
    }
}
