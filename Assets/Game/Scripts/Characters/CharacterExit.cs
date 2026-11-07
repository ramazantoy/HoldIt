using AF.Core;
using HoldIt.Restrooms;
using UnityEngine;
using VContainer;

namespace HoldIt.Characters
{
    /// <summary>
    /// How a character's view goes once the model has let the character go; its <see cref="CharacterView"/> hands over. A
    /// visitor sits on its stall's toilet, facing the door (<see cref="Sit"/>), until it is relieved: then it says so and
    /// goes in a puff (<see cref="Relieve"/>). After an accident it says so, spins and shrinks away (<see cref="Fail"/>).
    /// Only the body and the legs shrink, so the bubble over the head has its say before the view goes back to its pool.
    /// All of it runs on game time, without a tween.
    /// </summary>
    public sealed class CharacterExit : MonoBehaviour, IPoolable
    {
        [SerializeField, Required] private CharacterRig _rig;
        [SerializeField, Required] private SpeechBubble _bubble;

        private IGameClock _clock;
        private IRestrooms _restrooms;
        private CharacterSet _set;
        private PoolLife _life;
        private RestPose _rest;
        private Stage _stage;
        private StallVisit _visit;
        private Vector3 _sitFrom;
        private float _time;

        private enum Stage
        {
            None,
            Sitting,
            Puffing,
            Failing,
        }

        [Inject]
        public void Construct(IGameClock clock, CharacterSet set)
        {
            _clock = clock;
            _set = set;
        }

        /// <summary>The restrooms it sits in, until it is despawned; its view hands them over.</summary>
        public void Bind(IRestrooms restrooms) => _restrooms = restrooms;

        /// <summary>Gets from where it stands onto the stall's toilet and sits there until <see cref="Relieve"/>.</summary>
        public void Sit(StallVisit visit)
        {
            _visit = visit;
            _sitFrom = transform.position;
            Begin(Stage.Sitting);
        }

        public void Relieve()
        {
            _bubble.Show(BubbleLine.Relieved);
            Begin(Stage.Puffing);
        }

        public void Fail()
        {
            _bubble.Show(BubbleLine.Accident);
            Begin(Stage.Failing);
        }

        public void OnSpawned(PoolLife life)
        {
            _life = life;
            _stage = Stage.None;
            _visit = null;
            _rest.Scale(_rig, 1f);
        }

        public void OnDespawned()
        {
            _stage = Stage.None;
            _visit = null;
            _restrooms = null;
        }

        private void Awake() => _rest = RestPose.Of(_rig);

        private void LateUpdate()
        {
            switch (_stage)
            {
                case Stage.Sitting:
                    SitDown();
                    break;
                case Stage.Puffing:
                    Puff();
                    break;
                case Stage.Failing:
                    SpinAway();
                    break;
            }
        }

        private void Begin(Stage stage)
        {
            _stage = stage;
            _time = 0f;
        }

        private void SitDown()
        {
            var deltaTime = _clock.DeltaTime;
            _time += deltaTime;
            var sit = _set.Sit;
            var plan = _restrooms.LayoutOf(_visit.Gender).Stalls[_visit.Stall];
            var seat = plan.Toilet + plan.Rotation * Vector3.forward * sit.SeatOffset + Vector3.down * sit.Drop;
            var settled = Mathf.SmoothStep(0f, 1f, _time / sit.SettleSeconds);
            var turn = 1f - Mathf.Exp(-_set.TurnRate * deltaTime);
            transform.SetPositionAndRotation(Vector3.Lerp(_sitFrom, seat, settled),
                Quaternion.Slerp(transform.rotation, plan.Rotation, turn));
            _rest.Apply(_rig, WalkPose.Sitting(settled, sit), 0f);
        }

        private void Puff()
        {
            _time += _clock.DeltaTime;
            var going = ExitCurve.TryPuff(_time, _set.Exit, out var scale);
            _rest.Scale(_rig, scale);
            if (!going)
                DespawnOnceSaid();
        }

        private void SpinAway()
        {
            var deltaTime = _clock.DeltaTime;
            _time += deltaTime;
            var exit = _set.Exit;
            transform.Rotate(0f, exit.AccidentSpin * deltaTime, 0f, Space.World);
            var going = ExitCurve.TryShrink(_time, exit.AccidentSeconds, out var scale);
            _rest.Scale(_rig, scale);
            if (!going)
                DespawnOnceSaid();
        }

        private void DespawnOnceSaid()
        {
            if (_bubble.IsShowing)
                return;
            _stage = Stage.None;
            _life.Despawn();
        }
    }
}
