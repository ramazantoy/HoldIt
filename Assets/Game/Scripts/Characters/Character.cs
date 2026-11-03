using System;
using HoldIt.Board;
using HoldIt.Paths;
using HoldIt.Restrooms;

namespace HoldIt.Characters
{
    /// <summary>
    /// One character and its state machine. Idle until it is given a path, then Walking it cell by cell. Before every step
    /// it asks an <see cref="IStepGate"/>: a refused step (wrong door, too narrow) stops it where it is; a held cell makes
    /// it wait, and a wait past <c>blockedWait</c> seconds stops it too. It turns to each cell before asking for it, so a
    /// refused or waiting character faces where it wanted to go. It is Idle again at its path's end or where it stopped,
    /// and keeps facing the way it last turned. At its restroom's door it is told to queue (Queued, standing there) or to
    /// go into a stall (InStall): then it walks the stall's way in, off the grid and asking no gate, until it
    /// <see cref="IsInside"/>. All along its patience runs out (<see cref="LosePatience"/>), except in the stall.
    /// </summary>
    public sealed class Character : ICharacter
    {
        private readonly float _blockedWait;
        private readonly float _stallWalkSpeed;
        private CellPath _route;
        private int _index;
        private bool _stepping;
        private float _waited;

        /// <param name="speed">Tiles per second.</param>
        /// <param name="blockedWait">The longest wait, in seconds, before a held cell; past it the character stops.</param>
        /// <param name="stallWalkSpeed">World units per second along a stall's way in.</param>
        public Character(CharacterId id, Gender gender, BodySize size, Cell cell, float speed, float blockedWait,
            float stallWalkSpeed)
        {
            if (speed <= 0f)
                throw new ArgumentOutOfRangeException(nameof(speed), $"A character needs a positive speed, not {speed}.");
            if (blockedWait < 0f)
                throw new ArgumentOutOfRangeException(nameof(blockedWait), $"A wait cannot be negative ({blockedWait}).");
            if (stallWalkSpeed <= 0f)
                throw new ArgumentOutOfRangeException(nameof(stallWalkSpeed),
                    $"Walking into a stall needs a positive speed, not {stallWalkSpeed}.");

            Id = id;
            Gender = gender;
            Size = size;
            Speed = speed;
            _blockedWait = blockedWait;
            _stallWalkSpeed = stallWalkSpeed;
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
        public StallVisit Visit { get; private set; }
        public float VisitDistance { get; private set; }
        public float Patience { get; private set; }

        /// <summary>True once its patience has run out: an accident, unless it is already in a stall.</summary>
        public bool IsOutOfPatience => State != CharacterState.InStall && Patience >= 1f;

        /// <summary>True once an InStall character has walked all of its stall's way in.</summary>
        public bool IsInside => State == CharacterState.InStall && VisitDistance >= Visit.Line.Length;

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

        /// <summary>Waits at the restroom's door it stands on, holding it, until a stall is free. Throws unless Idle.</summary>
        public void Queue()
        {
            if (State != CharacterState.Idle)
                throw new InvalidOperationException($"Only an Idle character can queue; {Id} is {State}.");
            State = CharacterState.Queued;
        }

        /// <summary>
        /// Starts walking the stall's way in from the door it stands on. Throws unless Idle or Queued. Its cell stays the
        /// door; where it is from now on is on the visit's line.
        /// </summary>
        public void EnterStall(StallVisit visit)
        {
            if (visit == null)
                throw new ArgumentNullException(nameof(visit));
            if (State != CharacterState.Idle && State != CharacterState.Queued)
                throw new InvalidOperationException(
                    $"Only an Idle or queued character can go into a stall; {Id} is {State}.");

            Stop();
            Visit = visit;
            VisitDistance = 0f;
            State = CharacterState.InStall;
        }

        /// <summary>
        /// Uses up <paramref name="share"/> of its patience, a share of the whole; it stops at 1. Nothing happens InStall:
        /// a character in its stall waits no more.
        /// </summary>
        public void LosePatience(float share)
        {
            if (share < 0f)
                throw new ArgumentOutOfRangeException(nameof(share), $"Patience does not come back ({share}).");
            if (State != CharacterState.InStall)
                Patience = Math.Min(1f, Patience + share);
        }

        /// <summary>
        /// Moves for <paramref name="deltaTime"/> seconds: along the route while Walking, asking <paramref name="gate"/>
        /// before every step, or along the stall's way in while InStall. One call can cross several cells; what is left of
        /// the time carries on, into a wait as well. Returns why a step was given up during the call, or
        /// <see cref="StepRefusal.None"/>.
        /// </summary>
        public StepRefusal Advance(float deltaTime, IStepGate gate)
        {
            if (deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime), $"Time cannot run back ({deltaTime}).");
            if (gate == null)
                throw new ArgumentNullException(nameof(gate));

            if (State == CharacterState.InStall)
            {
                VisitDistance = Math.Min(Visit.Line.Length, VisitDistance + deltaTime * _stallWalkSpeed);
                return StepRefusal.None;
            }

            var refusal = StepRefusal.None;
            var time = deltaTime;
            while (State == CharacterState.Walking)
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
                        Stop();
                        refusal = verdict == StepVerdict.WrongDoor ? StepRefusal.WrongDoor : StepRefusal.TooWide;
                        break;
                    }
                    BeginStep();
                }

                var timeLeft = (1f - StepProgress) / Speed;
                if (time < timeLeft)
                {
                    StepProgress += time * Speed;
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
