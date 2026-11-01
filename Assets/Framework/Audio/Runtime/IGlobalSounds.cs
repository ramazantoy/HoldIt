namespace AF.Audio
{
    /// <summary>
    /// The root's sounds, for scene code whose sound must outlive its scene: a jingle that plays over the scene change.
    /// Sounds played through it stop only when they end, are stopped, or the app quits.
    /// </summary>
    public interface IGlobalSounds : ISounds
    {
    }
}
