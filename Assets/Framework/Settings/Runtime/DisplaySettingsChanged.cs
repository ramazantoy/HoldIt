namespace AF.Settings
{
    /// <summary>Published after the display settings in effect changed and were applied; not when a change leaves them as they were.</summary>
    public readonly struct DisplaySettingsChanged
    {
        public readonly DisplayState Previous;
        public readonly DisplayState Current;

        public DisplaySettingsChanged(DisplayState previous, DisplayState current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
