using AF.Core;
using UnityEngine;
using VContainer;

namespace HoldIt.Characters
{
    /// <summary>
    /// The puddle an accident leaves where it happened. It spreads out, stays and fades (<see cref="PuddleCurve"/>) on game
    /// time, then goes back to its pool by itself. Each one gets a size of its own between the look's smallest and largest.
    /// Only a sight: nobody is blocked by it.
    /// </summary>
    public sealed class PuddleView : MonoBehaviour, IPoolable
    {
        private static readonly int _baseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField, Required] private Renderer _renderer;

        private IGameClock _clock;
        private CharacterSet _set;
        private MaterialPropertyBlock _block;
        private PoolLife _life;
        private float _age;
        private float _size;

        [Inject]
        public void Construct(IGameClock clock, CharacterSet set)
        {
            _clock = clock;
            _set = set;
        }

        public void OnSpawned(PoolLife life)
        {
            _life = life;
            _age = 0f;
            var look = _set.PuddleLook;
            _size = Random.Range(look.MinSize, look.MaxSize);
            Apply(look.GrowFrom, 1f);
        }

        public void OnDespawned()
        {
        }

        private void LateUpdate()
        {
            _age += _clock.DeltaTime;
            if (!PuddleCurve.TryAt(_age, _set.PuddleLook, out var scale, out var alpha))
            {
                _life.Despawn();
                return;
            }
            Apply(scale, alpha);
        }

        private void Apply(float scale, float alpha)
        {
            var across = _size * scale;
            transform.localScale = new Vector3(across, 1f, across);
            var color = _set.PuddleLook.Color;
            color.a *= alpha;
            _block ??= new MaterialPropertyBlock();
            _renderer.GetPropertyBlock(_block);
            _block.SetColor(_baseColorId, color);
            _renderer.SetPropertyBlock(_block);
        }
    }
}
