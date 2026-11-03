using HoldIt.Board;

namespace HoldIt.Characters
{
    /// <summary>Lets a character into the next cell or holds it back; <see cref="Character.Advance"/> asks it every step.</summary>
    public interface IStepGate
    {
        /// <summary>
        /// Asked when the character is about to step from its cell into <paramref name="next"/>. On
        /// <see cref="StepVerdict.Enter"/> the gate holds <paramref name="next"/> for the character until it leaves it.
        /// </summary>
        StepVerdict TryEnter(ICharacter character, Cell next);

        /// <summary>The character finished a step out of <paramref name="cell"/>.</summary>
        void Leave(ICharacter character, Cell cell);
    }
}
