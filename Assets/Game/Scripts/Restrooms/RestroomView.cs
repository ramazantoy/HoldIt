using System;
using System.Collections.Generic;
using HoldIt.Board;
using MessagePipe;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace HoldIt.Restrooms
{
    /// <summary>
    /// Builds both restrooms' stalls (<see cref="IRestrooms.LayoutOf"/>) from the stall set, in world space, under a root of
    /// its own below this object: the partitions stretched to their length, and each stall's door, lamp and toilet. Each
    /// door and lamp starts from its stall's state and follows its changes (<see cref="StallStateChanged"/>). The view
    /// stays; a level builds its restrooms (<see cref="Build"/>) and takes them down again (<see cref="Clear"/>). Built
    /// restrooms are hidden (every piece at scale zero) until their <see cref="Pieces"/> are grown in.
    /// </summary>
    public sealed class RestroomView : MonoBehaviour
    {
        private const string ROOT_NAME = "Restrooms";

        private static readonly Gender[] _genders = { Gender.Male, Gender.Female };

        private readonly StallDoorView[][] _doors = new StallDoorView[_genders.Length][];
        private readonly StallLampView[][] _lamps = new StallLampView[_genders.Length][];
        private IRestrooms _restrooms;
        private StallSet _set;
        private IObjectResolver _resolver;
        private readonly List<Revealable> _pieces = new List<Revealable>();
        private IDisposable _subscription;
        private Transform _root;

        /// <summary>The standing restrooms' pieces, with the scale each has when shown.</summary>
        public IReadOnlyList<Revealable> Pieces => _pieces;

        public void Build(IRestrooms restrooms, StallSet set, IObjectResolver resolver, ISubscriber<StallStateChanged> changed)
        {
            Clear();
            _restrooms = restrooms;
            _set = set;
            _resolver = resolver;
            _root = new GameObject(ROOT_NAME).transform;
            _root.SetParent(transform, false);
            _subscription = changed.Subscribe(message => Show(message.Gender, message.Stall, message.State, false));

            foreach (var gender in _genders)
            {
                var layout = _restrooms.LayoutOf(gender);
                BuildPartitions(layout);
                BuildStalls(layout);
            }

            foreach (var piece in _pieces)
                piece.Transform.localScale = Vector3.zero;
        }

        /// <summary>Takes the restrooms down. They are gone at the end of the frame.</summary>
        public void Clear()
        {
            _subscription?.Dispose();
            _subscription = null;
            for (var i = 0; i < _genders.Length; i++)
            {
                _doors[i] = null;
                _lamps[i] = null;
            }

            _restrooms = null;
            _set = null;
            _resolver = null;
            _pieces.Clear();
            if (_root == null)
                return;
            _root.SetParent(null, false);
            Destroy(_root.gameObject);
            _root = null;
        }

        private void OnDestroy() => _subscription?.Dispose();

        /// <summary>A change before the stalls are built is skipped: building reads every stall's state.</summary>
        private void Show(Gender gender, int stall, StallState state, bool instantly)
        {
            if (_doors[(int)gender] == null)
                return;
            _doors[(int)gender][stall].Show(state, instantly);
            _lamps[(int)gender][stall].Show(state, instantly);
        }

        private void BuildPartitions(RestroomLayout layout)
        {
            var prefab = _set.Partition(layout.Gender);
            for (var i = 0; i < layout.Partitions.Count; i++)
            {
                var plan = layout.Partitions[i];
                var partition = _resolver.Instantiate(prefab, plan.Center, plan.Rotation, _root);
                var scale = partition.transform.localScale;
                partition.transform.localScale = new Vector3(scale.x, scale.y, scale.z * plan.Length);
                partition.name = $"{prefab.name} {i}";
                _pieces.Add(new Revealable(partition.transform, partition.transform.localScale));
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
                var door = _resolver.Instantiate(_set.Door(gender), stall.Hinge, stall.Rotation, _root);
                door.name = $"{_set.Door(gender).name} {i}";
                _doors[(int)gender][i] = door;
                _pieces.Add(new Revealable(door.transform, door.transform.localScale));

                var lamp = _resolver.Instantiate(_set.Lamp, stall.Lamp, stall.Rotation, _root);
                lamp.name = $"{_set.Lamp.name} {gender} {i}";
                _lamps[(int)gender][i] = lamp;
                _pieces.Add(new Revealable(lamp.transform, lamp.transform.localScale));

                var toilet = _resolver.Instantiate(_set.Toilet, stall.Toilet, stall.Rotation, _root);
                toilet.name = $"{_set.Toilet.name} {gender} {i}";
                _pieces.Add(new Revealable(toilet.transform, toilet.transform.localScale));

                Show(gender, i, _restrooms.StateOf(gender, i), true);
            }
        }
    }
}
