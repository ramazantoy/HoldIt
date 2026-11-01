namespace AF.Audio
{
    /// <summary>Published after a channel's volume changed, with the new value; not when it is set to the value it has.</summary>
    public readonly struct AudioVolumeChanged
    {
        public readonly AudioChannel Channel;
        public readonly float Volume;

        public AudioVolumeChanged(AudioChannel channel, float volume)
        {
            Channel = channel;
            Volume = volume;
        }
    }
}
