namespace AF.Platform
{
    /// <summary>Published when the store overlay opens or closes; not repeated for the same state.</summary>
    public readonly struct PlatformOverlayChanged
    {
        public readonly bool IsOpen;

        public PlatformOverlayChanged(bool isOpen) => IsOpen = isOpen;
    }
}
