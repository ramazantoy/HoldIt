using System;

namespace AF.Audio
{
    /// <summary>
    /// A volume the player can set (Master, Music…): a mixer group and the exposed parameter of its volume. The game defines
    /// its channels once (<c>Assets/Game/Scripts/AudioChannels.cs</c>) and passes them in <see cref="AudioOptions"/>.
    /// </summary>
    public sealed class AudioChannel
    {
        /// <param name="group">The mixer group's name, matched exactly.</param>
        /// <param name="volumeParameter">The exposed parameter of the group's volume.</param>
        public AudioChannel(string group, string volumeParameter)
        {
            if (string.IsNullOrEmpty(group))
                throw new ArgumentException("A channel needs its mixer group's name.", nameof(group));
            if (string.IsNullOrEmpty(volumeParameter))
                throw new ArgumentException("A channel needs the exposed parameter of its volume.", nameof(volumeParameter));
            Group = group;
            VolumeParameter = volumeParameter;
        }

        public string Group { get; }
        public string VolumeParameter { get; }

        public override string ToString() => Group;
    }
}
