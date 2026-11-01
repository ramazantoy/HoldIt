using System;
using System.Collections.Generic;
using System.Diagnostics;
using AF.Core;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace AF.Audio
{
    internal sealed class AudioVolumes : IAudioVolumes, IStartable, IDisposable
    {
        private const string UNFOCUSED_REASON = "unfocused";
        private const float SilentDecibels = -80f;
        private const float SilentVolume = 0.0001f;

        private readonly AudioSetup _setup;
        private readonly IPublisher<AudioVolumeChanged> _volumeChanged;
        private readonly AudioListenerAccess _listener;
        private readonly bool _followFocus;
        private readonly Dictionary<AudioChannel, float> _volumes = new Dictionary<AudioChannel, float>();
        private readonly List<Handle> _mutes = new List<Handle>();
        private readonly List<string> _muteReasons = new List<string>();
        private readonly IDisposable _subscription;
        private Handle _unfocused;
        private bool _muteWhenUnfocused;
        private bool _focused = true;
        private bool _started;
        private bool _disposed;

        [Inject]
        public AudioVolumes(AudioSetup setup, IPauseService pause, ISubscriber<PauseChanged> pauseChanged,
            IPublisher<AudioVolumeChanged> volumeChanged)
            : this(setup, pause, pauseChanged, volumeChanged, AudioListenerAccess.Unity,
                FollowsFocus(Application.isEditor, Application.isMobilePlatform))
        {
        }

        // Tests pass their own listener and leave focus alone.
        internal AudioVolumes(AudioSetup setup, IPauseService pause, ISubscriber<PauseChanged> pauseChanged,
            IPublisher<AudioVolumeChanged> volumeChanged, AudioListenerAccess listener, bool followFocus)
        {
            _setup = setup;
            _volumeChanged = volumeChanged;
            _listener = listener;
            _followFocus = followFocus;
            foreach (var channel in setup.Options.Channels)
                _volumes[channel] = 1f;

            _subscription = pauseChanged.Subscribe(change => ApplyPause(change.IsPaused));
            ApplyPause(pause.IsPaused);
            _muteWhenUnfocused = setup.Options.MuteWhenUnfocused;
            if (_followFocus)
            {
                Application.focusChanged += OnFocusChanged;
                _focused = Application.isFocused;
            }
            ApplyFocus();
        }

        public IReadOnlyList<AudioChannel> Channels => _setup.Options.Channels;

        public bool MuteWhenUnfocused
        {
            get => _muteWhenUnfocused;
            set
            {
                _muteWhenUnfocused = value;
                ApplyFocus();
            }
        }

        public float Get(AudioChannel channel) => _volumes[Known(channel)];

        public void Set(AudioChannel channel, float volume)
        {
            volume = Mathf.Clamp01(volume);
            if (_volumes[Known(channel)] == volume)
                return;
            _volumes[channel] = volume;
            // Before Start the mixer may ignore it (SetFloat in Awake does not stick); Start applies every channel.
            if (_started)
                Apply(channel);
            _volumeChanged.Publish(new AudioVolumeChanged(channel, volume));
        }

        public IDisposable Mute(string reason)
        {
            if (string.IsNullOrEmpty(reason))
                throw new ArgumentException("A mute request needs a reason.", nameof(reason));
            if (_disposed)
                throw new ObjectDisposedException(nameof(IAudioVolumes));

            var handle = new Handle(this);
            _mutes.Add(handle);
            _muteReasons.Add(reason);
            if (_mutes.Count == 1)
                ApplyMute();
            return handle;
        }

        public void Start()
        {
            _started = true;
            foreach (var channel in _setup.Options.Channels)
                Apply(channel);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _subscription.Dispose();
            if (_followFocus)
                Application.focusChanged -= OnFocusChanged;
            _mutes.Clear();
            _muteReasons.Clear();
            _listener.SetPause(false);
            _listener.SetVolume(1f);
        }

        /// <summary>
        /// Desktop players only. The Editor is unfocused whenever another tool has the keyboard. On mobile, focus goes with the
        /// on-screen keyboard and system overlays while the game stays visible, and Unity pauses the audio output itself in the
        /// background.
        /// </summary>
        internal static bool FollowsFocus(bool isEditor, bool isMobile) => !isEditor && !isMobile;

        internal static float ToDecibels(float volume) => volume <= SilentVolume ? SilentDecibels : 20f * Mathf.Log10(volume);

        internal void OnFocusChanged(bool focused)
        {
            _focused = focused;
            ApplyFocus();
        }

        private void Apply(AudioChannel channel) => _setup.SetParameter(channel.VolumeParameter, ToDecibels(_volumes[channel]));

        private void ApplyPause(bool paused)
        {
            CheckUntouched(_listener.GetPause() != _listener.LastPause, "AudioListener.pause");
            _listener.SetPause(paused);
        }

        private void ApplyMute()
        {
            CheckUntouched(!Mathf.Approximately(_listener.GetVolume(), _listener.LastVolume), "AudioListener.volume");
            var muted = _mutes.Count > 0;
            _listener.SetVolume(muted ? 0f : 1f);
            if (muted)
                Log.Info($"Audio muted: {string.Join(", ", _muteReasons)}");
            else
                Log.Info("Audio unmuted");
        }

        private void ApplyFocus()
        {
            if (_disposed)
                return;
            var mute = _followFocus && _muteWhenUnfocused && !_focused;
            if (mute && _unfocused == null)
            {
                _unfocused = (Handle)Mute(UNFOCUSED_REASON);
            }
            else if (!mute && _unfocused != null)
            {
                var handle = _unfocused;
                _unfocused = null;
                handle.Dispose();
            }
        }

        private void Release(Handle handle)
        {
            var index = _mutes.IndexOf(handle);
            if (index < 0)
                return;
            _mutes.RemoveAt(index);
            _muteReasons.RemoveAt(index);
            if (_mutes.Count == 0 && !_disposed)
                ApplyMute();
        }

        private AudioChannel Known(AudioChannel channel)
        {
            if (channel == null)
                throw new ArgumentNullException(nameof(channel));
            if (!_volumes.ContainsKey(channel))
                throw new ArgumentException($"The channel {channel} is not in the audio options.", nameof(channel));
            return channel;
        }

        // The listener's pause and volume are static and anyone can write them; the module is meant to be their only writer.
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        private static void CheckUntouched(bool changed, string property)
        {
            if (changed)
                Log.Error($"{property} was changed outside the Audio module. Pause through IPauseService and mute through IAudioVolumes.Mute.");
        }

        private sealed class Handle : IDisposable
        {
            private AudioVolumes _owner;

            public Handle(AudioVolumes owner) => _owner = owner;

            public void Dispose()
            {
                var releasing = _owner;
                _owner = null;
                releasing?.Release(this);
            }
        }
    }

    /// <summary>Reads and writes the static <see cref="AudioListener"/> state; tests use their own.</summary>
    internal class AudioListenerAccess
    {
        public static AudioListenerAccess Unity => new AudioListenerAccess();

        public bool LastPause { get; private set; }
        public float LastVolume { get; private set; } = 1f;

        public void SetPause(bool paused)
        {
            LastPause = paused;
            WritePause(paused);
        }

        public void SetVolume(float volume)
        {
            LastVolume = volume;
            WriteVolume(volume);
        }

        public virtual bool GetPause() => AudioListener.pause;
        public virtual float GetVolume() => AudioListener.volume;
        protected virtual void WritePause(bool paused) => AudioListener.pause = paused;
        protected virtual void WriteVolume(float volume) => AudioListener.volume = volume;
    }
}
