using System;
using HoldIt.Board;
using HoldIt.Paths;

namespace HoldIt.Characters
{
    /// <summary>
    /// One character and its state machine: Idle until it is given a path, then Walking it cell by cell, then Idle again
    /// on the last cell. A step begins at a cell boundary, so a rule can stop it there before it enters the next cell (M3).
    /// </summary>
    public sealed class Character : ICharacter
    {
        private CellPath _path;
        private int _index;
        private bool _stepping;

        public Character(CharacterId id, Gender gender, BodySize size, Cell cell, float speed)
        {
            if (speed <= 0f)
                throw new ArgumentOutOfRangeException(nameof(speed), $"A character needs a positive speed, not {speed}.");

            Id = id;
            Gender = gender;
            Size = size;
            Speed = speed;
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

        /// <summary>Starts walking the path. Refused (false) unless Idle and the path starts on this character's cell.</summary>
        public bool TryWalk(CellPath path)
        {
            if (path == null)
                throw new ArgumentNullException(nameof(path));
            if (State != CharacterState.Idle || path.Start != Cell)
                return false;

            _path = path;
            _index = 0;
            _stepping = false;
            State = CharacterState.Walking;
            return true;
        }

        /// <summary>
        /// Moves <see cref="Speed"/> × <paramref name="deltaTime"/> tiles along the path. One call can cross several cells;
        /// what is left of a step carries into the next one. Reaching the last cell makes the character Idle at once.
        /// </summary>
        public void Advance(float deltaTime)
        {
            if (deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime), $"Time cannot run back ({deltaTime}).");

            var distance = deltaTime * Speed;
            while (State == CharacterState.Walking)
            {
                if (!_stepping)
                {
                    if (_index >= _path.Count - 1)
                    {
                        Stop();
                        return;
                    }
                    if (distance <= 0f)
                        return;
                    BeginStep();
                }

                var remaining = 1f - StepProgress;
                if (distance < remaining)
                {
                    StepProgress += distance;
                    return;
                }

                distance -= remaining;
                EndStep();
            }
        }

        private void BeginStep()
        {
            Next = _path[_index + 1];
            if (Cell.TryDirectionTo(Next, out var direction))
                Facing = direction;
            StepProgress = 0f;
            _stepping = true;
        }

        private void EndStep()
        {
            Cell = Next;
            _index++;
            StepProgress = 0f;
            _stepping = false;
        }

        private void Stop()
        {
            _path = null;
            Next = Cell;
            State = CharacterState.Idle;
        }
    }
}
