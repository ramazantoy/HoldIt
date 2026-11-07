namespace HoldIt.Backdrop
{
    /// <summary>
    /// Which palette the backdrop is on, kept in the root scope so it outlasts the gameplay scene (and the menu in
    /// between). A new level takes the next palette from a shuffle bag, never the one it leaves; playing the same level
    /// again (a retry) keeps its palette.
    /// </summary>
    public interface IBackdropMemory
    {
        /// <summary>The palette the backdrop is on; -1 before the first level.</summary>
        int Current { get; }

        /// <summary>Moves to the palette of the level at <paramref name="levelIndex"/>, and returns it.</summary>
        int Choose(int levelIndex, int paletteCount);
    }
}
