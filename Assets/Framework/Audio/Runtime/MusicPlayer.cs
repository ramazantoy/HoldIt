using System;
using PrimeTween;
using UnityEngine;
using VContainer.Unity;

namespace AF.Audio
{
    /// <summary>Two looped sources under the audio rig: one fades in the current track while the other fades out the last one.</summary>
    internal sealed class MusicPlayer : IMusic, ITickable, IDisposable
    {
        private AudioSource _active;
        private AudioSource _idle;
        private Tween _activeFade;
        private Tween _idleFade;
        private bool _restartPending;
        private bool _disposed;

        public MusicPlayer(AudioRig rig, AudioSetup setup)
        {
            _active = CreateSource(rig, "Music A", setup);
            _idle = CreateSource(rig, "Music B", setup);
            AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
        }

        public AudioClip Current { get; private set; }

        public void Play(AudioClip clip, float fadeSeconds = 1f)
        {
            if (clip == null)
                throw new ArgumentNullException(nameof(clip), "Stop the music with Stop.");
            ThrowIfDisposed();
            if (clip == Current)
                return;

            // The idle source may still fade out an older track; that one is cut.
            var incoming = _idle;
            var outgoing = _active;
            _idleFade.Stop();
            _activeFade.Stop();
            _active = incoming;
            _idle = outgoing;
            _activeFade = default;
            _idleFade = default;
            Current = clip;

            incoming.Stop();
            incoming.clip = clip;
            incoming.volume = fadeSeconds > 0f ? 0f : 1f;
            incoming.Play();
            if (fadeSeconds > 0f)
                _activeFade = Tween.AudioVolume(incoming, 1f, fadeSeconds, useUnscaledTime: true);
            FadeOut(outgoing, fadeSeconds, isActive: false);
        }

        public void Stop(float fadeSeconds = 1f)
        {
            ThrowIfDisposed();
            if (Current == null)
                return;
            Current = null;
            _activeFade.Stop();
            FadeOut(_active, fadeSeconds, isActive: true);
        }

        public void Dispose()
        {
            _disposed = true;
            AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
            _activeFade.Stop();
            _idleFade.Stop();
        }

        private void FadeOut(AudioSource source, float fadeSeconds, bool isActive)
        {
            if (!source.isPlaying || fadeSeconds <= 0f || source.volume <= 0f)
            {
                Silence(source);
                return;
            }
            // By the time it ends, a newer Play may have given this source the current track: never silence that one.
            var fade = Tween.AudioVolume(source, 0f, fadeSeconds, useUnscaledTime: true)
                .OnComplete(() =>
                {
                    if (source != _active || Current == null)
                        Silence(source);
                }, warnIfTargetDestroyed: false);
            if (isActive)
                _activeFade = fade;
            else
                _idleFade = fade;
        }

        // An output device change or an audio reset stops every source. The callback comes from inside the reset, so the
        // current track starts again on the next frame.
        internal void OnAudioConfigurationChanged(bool deviceWasChanged) => _restartPending = true;

        public void Tick()
        {
            if (!_restartPending)
                return;
            _restartPending = false;
            if (Current != null && !_active.isPlaying)
                _active.Play();
        }

        private static void Silence(AudioSource source)
        {
            source.Stop();
            source.clip = null;
        }

        private static AudioSource CreateSource(AudioRig rig, string name, AudioSetup setup)
        {
            var source = rig.CreateChild(name).gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.volume = 0f;
            source.spatialBlend = 0f;
            // Music plays on while the game is paused; a game that wants it quieter there uses a mixer snapshot.
            source.ignoreListenerPause = true;
            source.outputAudioMixerGroup = setup.MusicGroup;
            return source;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(IMusic));
        }
    }
}
