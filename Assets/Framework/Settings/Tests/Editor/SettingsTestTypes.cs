using System;
using System.Collections.Generic;
using AF.Audio;
using AF.Input;
using UnityEngine;

namespace AF.Settings.Tests
{
    /// <summary>Records what the display settings write, in order, and never touches the Editor's screen or quality.</summary>
    internal sealed class FakeDisplay : IDisplayBackend
    {
        public readonly List<string> Calls = new List<string>();

        public bool IsMobile { get; set; }
        public bool SupportsExclusive { get; set; } = true;
        public Vector2Int DisplaySize { get; set; } = new Vector2Int(1920, 1080);
        public Vector2Int WorkArea { get; set; } = new Vector2Int(1920, 1040);
        public int DisplayRefreshRate { get; set; } = 144;

        public IEnumerable<Vector2Int> SupportedSizes { get; set; } = new[]
        {
            new Vector2Int(1280, 720), new Vector2Int(1920, 1080), new Vector2Int(1280, 720), new Vector2Int(2560, 1440),
            new Vector2Int(1600, 900), new Vector2Int(1920, 1080), new Vector2Int(800, 600),
        };

        public IReadOnlyList<string> QualityLevels { get; set; } = new[] { "Low", "PC" };
        public string StartupQuality { get; set; } = "PC";
        public bool Disposed { get; private set; }

        public void ApplyQuality(string level) => Calls.Add($"quality {level}");
        public void ApplyFramePacing(int vSyncCount, int targetFrameRate) => Calls.Add($"pacing {vSyncCount} {targetFrameRate}");
        public void ApplyWindow(Vector2Int size, WindowMode mode) => Calls.Add($"window {size.x}x{size.y} {mode}");
        public void Dispose() => Disposed = true;
    }

    internal sealed class FakeInputModes : IInputModeService
    {
        public InputMode Current => InputMode.None;
        public IReadOnlyList<InputMode> Requests => Array.Empty<InputMode>();
        public bool ConfineCursor { get; set; }

        public IDisposable Request(InputMode mode) => throw new NotSupportedException();
    }

    internal sealed class FakeVolumes : IAudioVolumes
    {
        private readonly Dictionary<AudioChannel, float> _volumes = new Dictionary<AudioChannel, float>();

        public FakeVolumes(AudioOptions options)
        {
            Channels = options.Channels;
            foreach (var channel in options.Channels)
                _volumes[channel] = 1f;
            MuteWhenUnfocused = options.MuteWhenUnfocused;
        }

        public IReadOnlyList<AudioChannel> Channels { get; }
        public bool MuteWhenUnfocused { get; set; }

        public float Get(AudioChannel channel) => _volumes[channel];
        public void Set(AudioChannel channel, float volume) => _volumes[channel] = Mathf.Clamp01(volume);
        public IDisposable Mute(string reason) => throw new NotSupportedException();
    }
}
