namespace HoldIt.Input
{
    /// <summary>
    /// The player asked to go back: Escape, or the Android back button, which arrives as Escape. Published from UI/Cancel, the
    /// game's only back action, so one press never does two things. Each screen decides what back means.
    /// </summary>
    public readonly struct BackPressed
    {
    }
}
