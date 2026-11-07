using System;
using System.Threading;
using AF.Core;
using Cysharp.Threading.Tasks;
using HoldIt.Levels;
using PrimeTween;
using UnityEngine;
using VContainer.Unity;

namespace HoldIt.Backdrop
{
    /// <summary>
    /// Paints the scene's backdrop as a level opens (in Initialize, so the first frame of the level already has it): it makes the scene's skybox from the config's material and changes
    /// it from the palette the last level left (the start colours for the first) into the level's own, which
    /// <see cref="IBackdropMemory"/> picks (<see cref="BackdropTransition"/> times it). The camera's clear flags
    /// must be Skybox. The material is the scene's own copy, and goes with the scene.
    /// </summary>
    internal sealed class BackdropPlayer : IInitializable, IDisposable
    {
        private static readonly int _top = Shader.PropertyToID("_Top");
        private static readonly int _bottom = Shader.PropertyToID("_Bottom");

        private readonly BackdropConfig _config;
        private readonly ILevelCursor _cursor;
        private readonly IBackdropMemory _memory;
        private readonly CancellationTokenSource _life = new CancellationTokenSource();
        private Material _material;

        public BackdropPlayer(BackdropConfig config, ILevelCursor cursor, IBackdropMemory memory)
        {
            _config = config;
            _cursor = cursor;
            _memory = memory;
        }

        public void Initialize()
        {
            _material = new Material(_config.Material);
            RenderSettings.skybox = _material;

            var previous = _memory.Current;
            var from = previous >= 0 && previous < _config.PaletteCount ? _config.PaletteAt(previous) : _config.Start;
            var to = _config.PaletteAt(_memory.Choose(_cursor.Index, _config.PaletteCount));
            Apply(from);

            var seconds = BackdropTransition.Duration(from, to, _config.MinSeconds, _config.MaxSeconds, _config.FullDistance);
            Change(from, to, seconds).Forget();
        }

        public void Dispose()
        {
            _life.Cancel();
            _life.Dispose();
            if (RenderSettings.skybox == _material)
                RenderSettings.skybox = null;
            if (_material != null)
                UnityEngine.Object.Destroy(_material);
        }

        private async UniTask Change(BackdropColors from, BackdropColors to, float seconds)
        {
            await Tween.Custom(0f, 1f, seconds, t => Apply(BackdropColors.Lerp(from, to, t)), Ease.InOutSine, useUnscaledTime: true)
                .ToUniTask(_life.Token);
        }

        private void Apply(BackdropColors colors)
        {
            _material.SetColor(_top, colors.Top);
            _material.SetColor(_bottom, colors.Bottom);
        }
    }
}
