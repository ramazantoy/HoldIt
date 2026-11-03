using AF.Core;
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

        [Inject]
        public void Construct(StallSet set) => _set = set;

        public void Show(StallState state)
        {
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
