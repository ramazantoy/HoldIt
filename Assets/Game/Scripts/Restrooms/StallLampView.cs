using AF.Core;
using PrimeTween;
using UnityEngine;
using VContainer;

namespace HoldIt.Restrooms
{
    /// <summary>
    /// A stall's lamp: green while the stall is free, red otherwise, with a soft light of the same colour on the floor in
    /// front of it. Colours go through property blocks, so the shared materials stay as they are.
    /// </summary>
    public sealed class StallLampView : MonoBehaviour
    {
        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField, Required] private Renderer _bulb;
        [SerializeField, Required] private Renderer _glow;

        private StallSet _set;
        private MaterialPropertyBlock _block;
        private Tween _pulse;

        [Inject]
        public void Construct(StallSet set) => _set = set;

        /// <summary>Shows the state; unless <paramref name="instantly"/> the bulb also gives a pulse for the change.</summary>
        public void Show(StallState state, bool instantly)
        {
            if (!instantly)
            {
                if (_pulse.isAlive)
                    _pulse.Complete();
                _pulse = Tween.PunchScale(_bulb.transform, Vector3.one * _set.LampPunch, _set.LampPunchSeconds, frequency: 1)
                    .SetCancellationToken(destroyCancellationToken);
            }

            _block ??= new MaterialPropertyBlock();
            var color = state == StallState.Free ? _set.FreeColor : _set.BusyColor;

            _block.SetColor(_baseColorId, color);
            _bulb.SetPropertyBlock(_block);
            color.a = _set.GlowAlpha;
            _block.SetColor(_baseColorId, color);
            _glow.SetPropertyBlock(_block);
        }
    }
}
