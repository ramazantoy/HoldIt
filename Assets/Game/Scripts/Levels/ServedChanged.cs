namespace HoldIt.Levels
{
    /// <summary>One more character made it into a stall: the level's score, <see cref="ILevelRun.Served"/>.</summary>
    public readonly struct ServedChanged
    {
        public readonly int Served;

        public ServedChanged(int served) => Served = served;
    }
}
