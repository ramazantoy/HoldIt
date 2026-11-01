namespace AF.Input
{
    /// <summary>Published after the active mode changed and its maps are applied; not on every request.</summary>
    public readonly struct InputModeChanged
    {
        public readonly InputMode Previous;
        public readonly InputMode Current;

        public InputModeChanged(InputMode previous, InputMode current)
        {
            Previous = previous;
            Current = current;
        }
    }
}
