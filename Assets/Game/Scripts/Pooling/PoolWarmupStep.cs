using System;
using System.Collections.Generic;
using System.Threading;
using AF.Core;
using Cysharp.Threading.Tasks;
using HoldIt.Characters;
using VContainer;

namespace HoldIt.Pooling
{
    /// <summary>
    /// A boot step: warms the root's pools of every pooled prefab one after the other, a frame apart, so the intro video
    /// does not stall on any one of them. The intro waits for the boot, and the video is long enough to hide the work.
    /// </summary>
    internal sealed class PoolWarmupStep : IBootStep
    {
        private readonly IPools _pools;
        private readonly IReadOnlyList<WarmupItem> _items;
        private readonly Func<CancellationToken, UniTask> _nextFrame;

        [Inject]
        public PoolWarmupStep(IPersistentPools pools, CharacterSet set, PoolWarmup warmup)
            : this(pools, Items(set, warmup), token => UniTask.NextFrame(token))
        {
        }

        // Tests pass their own items and frames.
        internal PoolWarmupStep(IPools pools, IReadOnlyList<WarmupItem> items, Func<CancellationToken, UniTask> nextFrame)
        {
            _pools = pools;
            _items = items;
            _nextFrame = nextFrame;
        }

        public async UniTask Run(CancellationToken cancellation)
        {
            foreach (var item in _items)
            {
                cancellation.ThrowIfCancellationRequested();
                if (item.Count <= 0)
                    continue;
                item.Run(_pools);
                await _nextFrame(cancellation);
            }
        }

        private static IReadOnlyList<WarmupItem> Items(CharacterSet set, PoolWarmup warmup)
        {
            var items = new List<WarmupItem>();
            foreach (Gender gender in Enum.GetValues(typeof(Gender)))
            {
                foreach (BodySize size in Enum.GetValues(typeof(BodySize)))
                    items.Add(WarmupItem.Of(set.Prefab(gender, size), warmup.Characters));
            }
            items.Add(WarmupItem.Of(warmup.Drawing.Marker, warmup.Markers));
            items.Add(WarmupItem.Of(set.SpeechBubble, warmup.SpeechBubbles));
            items.Add(WarmupItem.Of(set.FloatText, warmup.FloatTexts));
            items.Add(WarmupItem.Of(set.Puff, warmup.Puffs));
            items.Add(WarmupItem.Of(set.Dust, warmup.Dusts));
            items.Add(WarmupItem.Of(set.Splash, warmup.Splashes));
            items.Add(WarmupItem.Of(set.Sparkle, warmup.Sparkles));
            items.Add(WarmupItem.Of(set.Sweat, warmup.Sweats));
            items.Add(WarmupItem.Of(set.Puddle, warmup.Puddles));
            return items;
        }
    }
}
