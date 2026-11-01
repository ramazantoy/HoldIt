using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AF.Audio;
using AF.Core;
using AF.Save;

namespace AF.Settings
{
    /// <summary>
    /// Channel volumes and the unfocused mute, a framework-owned section of this device's save data. Volumes are keyed by
    /// mixer group name. Fields are only ever added; a breaking change uses a new key.
    /// </summary>
    public sealed class VolumeSettingsData
    {
        public Dictionary<string, float> volumes = new Dictionary<string, float>();

        /// <summary>Null: the player has not chosen; <c>AudioOptions.MuteWhenUnfocused</c> applies.</summary>
        public bool? muteWhenUnfocused;
    }

    /// <summary>
    /// Saves <see cref="IAudioVolumes"/> with this device's data and sets it back at startup. Keeps no state of its own:
    /// whatever sets a volume through <see cref="IAudioVolumes"/>, the value is saved.
    /// </summary>
    internal sealed class VolumeSettings : Savable<VolumeSettingsData>
    {
        private const float DefaultVolume = 1f;

        private readonly IAudioVolumes _volumes;
        private readonly AudioOptions _options;

        public VolumeSettings(IAudioVolumes volumes, AudioOptions options)
        {
            _volumes = volumes;
            _options = options;
        }

        public override string Key => "settings.audio";
        public override SaveDocument Document => SaveDocument.Device;

        protected override VolumeSettingsData Capture()
        {
            var data = new VolumeSettingsData();
            foreach (var channel in _volumes.Channels)
                data.volumes[channel.Group] = _volumes.Get(channel);
            if (_volumes.MuteWhenUnfocused != _options.MuteWhenUnfocused)
                data.muteWhenUnfocused = _volumes.MuteWhenUnfocused;
            return data;
        }

        protected override void Restore(VolumeSettingsData data)
        {
            // A file with "volumes": null replaces the field initializer with null.
            var saved = data.volumes ?? new Dictionary<string, float>();
            foreach (var channel in _volumes.Channels)
            {
                var volume = saved.TryGetValue(channel.Group, out var value) && !float.IsNaN(value) ? value : DefaultVolume;
                _volumes.Set(channel, volume);
            }
            var unknown = saved.Keys.Where(group => _volumes.Channels.All(channel => channel.Group != group)).ToList();
            if (unknown.Count > 0)
                Log.Warning($"Settings: no audio channel for the saved volume(s) of {string.Join(", ", unknown)}; dropped.");
            _volumes.MuteWhenUnfocused = data.muteWhenUnfocused ?? _options.MuteWhenUnfocused;

            Log.Info($"Settings: volumes {string.Join(", ", _volumes.Channels.Select(channel => $"{channel.Group} {_volumes.Get(channel).ToString("0.##", CultureInfo.InvariantCulture)}"))}, " +
                     $"mute when unfocused {(_volumes.MuteWhenUnfocused ? "on" : "off")}");
        }
    }
}
