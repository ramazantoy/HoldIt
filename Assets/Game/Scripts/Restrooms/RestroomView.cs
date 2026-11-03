using System;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace HoldIt.Restrooms
{
    /// <summary>
    /// Builds both restrooms' stalls (<see cref="IRestrooms.LayoutOf"/>) from the stall set, in world space, under this
    /// object: the partitions stretched to their length, and each stall's door, lamp and toilet. Each door and lamp starts
    /// from its stall's state and follows its changes (<see cref="StallStateChanged"/>).
    /// </summary>
    public sealed class RestroomView : MonoBehaviour
    {
        private static readonly Gender[] _genders = { Gender.Male, Gender.Female };

        private readonly StallDoorView[][] _doors = new StallDoorView[_genders.Length][];
        private readonly StallLampView[][] _lamps = new StallLampView[_genders.Length][];
        private IRestrooms _restrooms;
        private StallSet _set;
        private IObjectResolver _resolver;
        private IDisposable _subscription;

        [Inject]
        public void Construct(IRestrooms restrooms, StallSet set, IObjectResolver resolver,
            ISubscriber<StallStateChanged> changed)
        {
            _restrooms = restrooms;
            _set = set;
            _resolver = resolver;
            _subscription = changed.Subscribe(message => Show(message.Gender, message.Stall, message.State, false));
        }

        private void Start()
        {
            foreach (var gender in _genders)
            {
                var layout = _restrooms.LayoutOf(gender);
                BuildPartitions(layout);
                BuildStalls(layout);
            }
        }

        private void OnDestroy() => _subscription?.Dispose();

        /// <summary>A change before the stalls are built is skipped: building reads every stall's state.</summary>
        private void Show(Gender gender, int stall, StallState state, bool instantly)
        {
            if (_doors[(int)gender] == null)
                return;
            _doors[(int)gender][stall].Show(state, instantly);
            _lamps[(int)gender][stall].Show(state);
        }

        private void BuildPartitions(RestroomLayout layout)
        {
            var prefab = _set.Partition(layout.Gender);
            for (var i = 0; i < layout.Partitions.Count; i++)
            {
                var plan = layout.Partitions[i];
                var partition = _resolver.Instantiate(prefab, plan.Center, plan.Rotation, transform);
                var scale = partition.transform.localScale;
                partition.transform.localScale = new Vector3(scale.x, scale.y, scale.z * plan.Length);
                partition.name = $"{prefab.name} {i}";
            }
        }

        private void BuildStalls(RestroomLayout layout)
        {
            var gender = layout.Gender;
            var count = layout.Stalls.Count;
            _doors[(int)gender] = new StallDoorView[count];
            _lamps[(int)gender] = new StallLampView[count];
            for (var i = 0; i < count; i++)
            {
                var stall = layout.Stalls[i];
                var door = _resolver.Instantiate(_set.Door(gender), stall.Hinge, stall.Rotation, transform);
                door.name = $"{_set.Door(gender).name} {i}";
                _doors[(int)gender][i] = door;

                var lamp = _resolver.Instantiate(_set.Lamp, stall.Lamp, stall.Rotation, transform);
                lamp.name = $"{_set.Lamp.name} {gender} {i}";
                _lamps[(int)gender][i] = lamp;

                var toilet = _resolver.Instantiate(_set.Toilet, stall.Toilet, stall.Rotation, transform);
                toilet.name = $"{_set.Toilet.name} {gender} {i}";

                Show(gender, i, _restrooms.StateOf(gender, i), true);
            }
        }
    }
}
