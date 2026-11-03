using AF.Core;
using UnityEngine;
using VContainer;

namespace HoldIt.Restrooms
{
    /// <summary>
    /// A stall's door. Its root stands at the hinge; the panel runs along local -X from it and swings out of the stall
    /// (toward +Z) as the hinge turns. Shut while the visitor is in, open otherwise: <see cref="Show"/> sets which, and
    /// every frame the door eases toward it with game time.
    /// </summary>
    public sealed class StallDoorView : MonoBehaviour
    {
        [SerializeField, Required, Tooltip("Turns about Y to open the door; the panel hangs under it.")]
        private Transform _hinge;

        private IGameClock _clock;
        private StallSet _set;
        private float _target;
        private float _angle;

        [Inject]
        public void Construct(IGameClock clock, StallSet set)
        {
            _clock = clock;
            _set = set;
        }

        /// <summary>Swings toward the stall's state; <paramref name="instantly"/> puts it there at once.</summary>
        public void Show(StallState state, bool instantly)
        {
            _target = state == StallState.Shut ? 0f : _set.DoorOpenAngle;
            if (!instantly)
                return;
            _angle = _target;
            Apply();
        }

        private void LateUpdate()
        {
            if (Mathf.Approximately(_angle, _target))
                return;
            _angle = Mathf.Lerp(_angle, _target, 1f - Mathf.Exp(-_set.DoorRate * _clock.DeltaTime));
            Apply();
        }

        private void Apply() => _hinge.localRotation = Quaternion.Euler(0f, _angle, 0f);
    }
}
