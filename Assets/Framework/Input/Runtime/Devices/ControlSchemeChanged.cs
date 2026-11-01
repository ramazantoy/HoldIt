namespace AF.Input
{
    /// <summary>
    /// Published when the player starts using another control scheme of the actions asset (keyboard and mouse, gamepad,
    /// touch). Switching between devices of the same scheme publishes nothing.
    /// </summary>
    public readonly struct ControlSchemeChanged
    {
        /// <summary>Null when no scheme was known yet.</summary>
        public readonly string Previous;

        public readonly string Current;

        public ControlSchemeChanged(string previous, string current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
