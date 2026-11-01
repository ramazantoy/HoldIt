using System;
using System.Collections.Generic;
using AF.Core;
using PrimeTween;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace AF.Audio
{
    /// <summary>
    /// Plays cues on pooled sources of the root and takes each source back when its sound ends. Every play records the
    /// scope's <see cref="ScopeSounds"/> that asked for it, so a scope's sounds can stop with it.
    /// </summary>
    internal sealed class VoicePlayer : ITickable, IDisposable
    {
        private const string TEMPLATE_NAME = "Voice";

        private readonly IPool<SoundVoice> _pool;
        private readonly Transform _playingParent;
        private readonly Func<bool> _listenerPaused;
        private readonly List<Playing> _playing = new List<Playing>();
        private readonly HashSet<SoundCue> _reportedCues = new HashSet<SoundCue>();

        [Inject]
        public VoicePlayer(IPools pools, AudioRig rig, AudioOptions options)
            : this(pools, rig, options, () => AudioListener.pause)
        {
        }

        // Tests pass their own listener state.
        internal VoicePlayer(IPools pools, AudioRig rig, AudioOptions options, Func<bool> listenerPaused)
        {
            _listenerPaused = listenerPaused;
            // Playing voices sit under the rig; waiting ones under the root's [Pools].
            _playingParent = rig.Root;
            var template = rig.CreateChild(TEMPLATE_NAME, active: false);
            var source = template.gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            _pool = pools.For(template.gameObject.AddComponent<SoundVoice>());
            _pool.Prewarm(options.PrewarmVoices);
        }

        /// <summary>Sounds playing now.</summary>
        public int Count => _pool.CountActive;

        public SoundHandle Play(SoundCue cue, Vector3? position, object owner)
        {
            if (cue == null)
                throw new ArgumentNullException(nameof(cue));
            var clip = cue.PickClip();
            if (clip == null)
            {
                // Validation reports the cue too; once per cue here, so a sound played every frame does not flood the log.
                if (_reportedCues.Add(cue))
                    Log.Error($"The sound cue {cue.name} has no clip to play.", cue);
                return default;
            }

            var voice = position.HasValue ? _pool.Spawn(position.Value, Quaternion.identity, _playingParent) : _pool.Spawn(_playingParent);
            var life = PooledObject.Of(voice).Life;
            var source = voice.Source;
            // Every field, every time: a pooled source keeps what its previous sound set.
            source.clip = clip;
            source.outputAudioMixerGroup = cue.Output;
            source.volume = cue.Volume;
            source.pitch = cue.PickPitch();
            source.loop = cue.Loop;
            source.ignoreListenerPause = cue.PlayWhilePaused;
            source.spatialBlend = position.HasValue ? 1f : 0f;
            source.minDistance = cue.MinDistance;
            source.maxDistance = cue.MaxDistance;
            source.rolloffMode = cue.Rolloff;
            voice.Fade = default;
            voice.Stopping = false;
            source.Play();

            _playing.Add(new Playing(voice, life, owner));
            return new SoundHandle(this, voice, life);
        }

        public void Stop(SoundVoice voice, PoolLife life, float fadeSeconds)
        {
            if (!life.IsCurrent)
                return;
            // Not awaited: stopping it is safe.
            voice.Fade.Stop();
            var source = voice.Source;
            if (fadeSeconds <= 0f || source.volume <= 0f)
            {
                life.Despawn();
                return;
            }
            // Tick takes the voice back once the fade is over, however it ended: its completion is not the only way back.
            voice.Stopping = true;
            voice.Fade = Tween.AudioVolume(source, 0f, fadeSeconds, useUnscaledTime: source.ignoreListenerPause)
                .SetCancellationToken(life.Token);
        }

        /// <summary>Stops at once every sound <paramref name="owner"/> played.</summary>
        public void StopOwned(object owner)
        {
            foreach (var playing in _playing.ToArray())
            {
                if (playing.Owner == owner)
                    playing.Life.Despawn();
            }
            _playing.RemoveAll(playing => playing.Owner == owner);
        }

        public void Tick()
        {
            var listenerPaused = _listenerPaused();
            for (var i = _playing.Count - 1; i >= 0; i--)
            {
                var playing = _playing[i];
                if (!playing.Life.IsCurrent)
                {
                    _playing.RemoveAt(i);
                    continue;
                }
                var voice = playing.Voice;
                if (voice.Stopping)
                {
                    // Fading out (frozen with the game while paused) until the fade ends or something kills it.
                    if (voice.Fade.isAlive)
                        continue;
                    playing.Life.Despawn();
                    _playing.RemoveAt(i);
                    continue;
                }
                var source = voice.Source;
                // Paused with the game, or still loading its clip: not over yet.
                if (source.isPlaying || listenerPaused && !source.ignoreListenerPause ||
                    source.clip != null && source.clip.loadState == AudioDataLoadState.Loading)
                    continue;
                playing.Life.Despawn();
                _playing.RemoveAt(i);
            }
        }

        // The root's pools are disposed before this (Scoped before singletons) and took the sources with them.
        public void Dispose() => _playing.Clear();

        private readonly struct Playing
        {
            public readonly SoundVoice Voice;
            public readonly PoolLife Life;
            public readonly object Owner;

            public Playing(SoundVoice voice, PoolLife life, object owner)
            {
                Voice = voice;
                Life = life;
                Owner = owner;
            }
        }
    }
}
