namespace HoldIt.Haptics
{
    /// <summary>
    /// Makes the phone vibrate, if the player allows it (<c>IGameSettings.Haptics</c>). Game code asks for it through
    /// <c>IFeedback</c>, which pairs it with the sound of the same event.
    /// </summary>
    public interface IHaptics
    {
        void Play(HapticStrength strength);
    }
}
