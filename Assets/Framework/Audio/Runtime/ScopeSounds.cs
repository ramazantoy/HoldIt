using System;
using UnityEngine;

namespace AF.Audio
{
    /// <summary>
    /// Owns the sounds played through it and stops them when disposed. Registered in the root as Scoped (<see cref="ISounds"/>,
    /// one per scope) and once more as a singleton (<see cref="IGlobalSounds"/>).
    /// </summary>
    internal sealed class ScopeSounds : IGlobalSounds, IDisposable
    {
        private readonly VoicePlayer _player;
        private bool _disposed;

        public ScopeSounds(VoicePlayer player) => _player = player;

        public SoundHandle Play(SoundCue cue)
        {
            ThrowIfDisposed();
            return _player.Play(cue, null, this);
        }

        public SoundHandle PlayAt(SoundCue cue, Vector3 position)
        {
            ThrowIfDisposed();
            return _player.Play(cue, position, this);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _player.StopOwned(this);
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ISounds), "The scope that played through this instance is gone.");
        }
    }
}
