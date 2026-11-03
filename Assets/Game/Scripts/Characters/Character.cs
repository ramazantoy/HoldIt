using System;
using HoldIt.Board;
using HoldIt.Paths;

namespace HoldIt.Characters
{
    /// <summary>
    /// One character and its state machine. Idle until it is given a path, then Walking it cell by cell. Before every step
    /// it asks an <see cref="IStepGate"/>: a refused step (wrong door, too narrow) turns it back along the cells it walked
    /// (Returning); a held cell makes it wait, and a wait past <c>blockedWait</c> seconds stops it where it is. It turns to
    /// each cell before asking for it, so a refused or waiting character faces where it wanted to go. It is Idle again at
    /// its path's end, back where it began, or where it stopped, and keeps facing the way it last turned.
    /// </summary>
    public sealed class Character : ICharacter
    {
        private readonly float _blockedWait;
        private readonly float _returnSpeedFactor;
        private CellPath _route;
        private int _index;
        private bool _stepping;
        private float _waited;

        /// <param name="speed">Tiles per second.</param>
        /// <param name="blockedWait">The longest wait, in seconds, before a held cell; past it the character stops.</param>
        /// <param name="returnSpeedFactor">How much faster than <paramref name="speed"/> it walks back.</param>
        public Character(CharacterId id, Gender gender, BodySize size, Cell cell, float speed, float blockedWait,
            float returnSpeedFactor)
        {
            if (speed <= 0f)
                throw new ArgumentOutOfRangeException(nameof(speed), $"A character needs a positive speed, not {speed}.");
            if (blockedWait < 0f)
                throw new ArgumentOutOfRangeException(nameof(blockedWait), $"A wait cannot be negative ({blockedWait}).");
            if (returnSpeedFactor <= 0f)
                throw new ArgumentOutOfRangeException(nameof(returnSpeedFactor),
                    $"Walking back needs a positive speed factor, not {returnSpeedFactor}.");

            Id = id;
            Gender = gender;
            Size = size;
            Speed = speed;
            _blockedWait = blockedWait;
            _returnSpeedFactor = returnSpeedFactor;
            Cell = cell;
            Next = cell;
        }

        public CharacterId Id { get; }
        public Gender Gender { get; }
        public BodySize Size { get; }
        public float Speed { get; }
        public CharacterState State { get; private set; } = CharacterState.Idle;
        public Cell Cell { get; private set; }
        public Cell Next { get; private set; }
        public float StepProgress { get; private set; }
        public Direction Facing { get; private set; } = Direction.South;
        public CellPath Route => _route;
        public int RouteIndex => _index;

        /// <summary>Starts walking the path. Refused (false) unless Idle and the path starts on this character's cell.</summary>
        public bool TryWalk(CellPath path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (State != CharacterState.Idle || path.Start != Cell)
                return false;

            _route = path;
            _index = 0;
            _stepping = false;
            _waited = 0f;
            State = CharacterState.Walking;
            return true;
        }

        /// <summary>
        /// Moves along the route for <paramref name="deltaTime"/> seconds, asking <paramref name="gate"/> before every step.
        /// One call can cross several cells; what is left of the time carries on, into a wait as well. Returns why a step
        /// was given up during the call, or <see cref="StepRefusal.None"/>.
        /// </summary>
        public StepRefusal Advance(float deltaTime, IStepGate gate)
        {
            if (deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime), $"Time cannot run back ({deltaTime}).");
            if (gate == null)
                throw new ArgumentNullException(nameof(gate));

            var refusal = StepRefusal.None;
            var time = deltaTime;
            while (State != CharacterState.Idle)
            {
                if (!_stepping)
                {
                    if (_index >= _route.Count - 1)
                    {
                        Stop();
                        break;
                    }
                    if (time <= 0f)
                        break;

                    var next = _route[_index + 1];
                    if (Cell.TryDirectionTo(next, out var toward))
                        Facing = toward;
                    var verdict = gate.TryEnter(this, next);
                    if (verdict == StepVerdict.Wait)
                    {
                        _waited += time;
                        if (_waited >= _blockedWait)
                        {
                            Stop();
                            refusal = StepRefusal.Blocked;
                        }
                        break;
                    }
                    if (verdict != StepVerdict.Enter)
                    {
                        refusal = verdict == StepVerdict.WrongDoor ? StepRefusal.WrongDoor : StepRefusal.TooWide;
                        TurnBack();
                        continue;
                    }
                    BeginStep();
                }

                var speed = State == CharacterState.Returning ? Speed * _returnSpeedFactor : Speed;
                var timeLeft = (1f - StepProgress) / speed;
                if (time < timeLeft)
                {
                    StepProgress += time * speed;
                    break;
                }

                time -= timeLeft;
                EndStep(gate);
            }
            return refusal;
        }

        private void BeginStep()
        {
            _waited = 0f;
            Next = _route[_index + 1];
            StepProgress = 0f;
            _stepping = true;
        }

        private void EndStep(IStepGate gate)
        {
            var left = Cell;
            Cell = Next;
            _index++;
            StepProgress = 0f;
            _stepping = false;
            gate.Leave(this, left);
        }

        /// <summary>Walks back over the cells walked so far; refused on the first step, it simply stays.</summary>
        private void TurnBack()
        {
            if (_index == 0)
            {
                Stop();
                return;
            }

            var cells = new Cell[_index + 1];
            for (var i = 0; i <= _index; i++)
                cells[i] = _route[_index - i];
            _route = new CellPath(cells);
            _index = 0;
            _waited = 0f;
            State = CharacterState.Returning;
        }

        private void Stop()
        {
            _route = null;
            _index = 0;
            _stepping = false;
            _waited = 0f;
            Next = Cell;
            StepProgress = 0f;
            State = CharacterState.Idle;
        }
    }
}
