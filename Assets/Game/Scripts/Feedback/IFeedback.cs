namespace HoldIt.Feedback
{
    /// <summary>
    /// The one way game code makes its events heard and felt: it says what happened, and the <see cref="FeedbackSet"/>
    /// decides the sound and the vibration. Every scope has its own, so a scene's sounds stop with the scene.
    /// </summary>
    public interface IFeedback
    {
        void Play(FeedbackKind kind);
    }
}
