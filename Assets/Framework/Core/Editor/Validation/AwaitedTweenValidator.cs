using System;
using System.Collections.Generic;
using System.Reflection;

namespace AF.Core.Editor
{
    /// <summary>
    /// Finds project code that awaits a PrimeTween tween or sequence some other way than <c>ToUniTask(cancellation)</c>.
    /// PrimeTween's own await returns normally when the tween was stopped, or when its token was cancelled before the await
    /// resumed, and the code after it runs as if the animation had finished. Project assemblies only (Assets/), tests and
    /// editor code excluded; code marked <see cref="TweenAwaitExemptAttribute"/> is skipped.
    /// </summary>
    public sealed class AwaitedTweenValidator : IValidator
    {
        // Matched by name: the editor assembly does not reference PrimeTween.
        private const string TWEEN = "PrimeTween.Tween";
        private const string SEQUENCE = "PrimeTween.Sequence";
        private const string UNITASK = "Cysharp.Threading.Tasks.UniTask";
        private const string ADVICE = "Await tweens with .ToUniTask(cancellation): cancelling stops the tween and the await throws, " +
                                      "instead of returning as if the animation had finished. Mark a deliberate exception " +
                                      "[TweenAwaitExempt(\"reason\")].";

        public void ValidateProject(ValidationReport report) => Check(DirectCallScan.ProjectTypes(), report);

        internal static void Check(IEnumerable<Type> types, ValidationReport report) =>
            DirectCallScan.Check(types, typeof(TweenAwaitExemptAttribute), Describe, ADVICE, report, DescribeBox);

        /// <summary>The call as reported, or null when it is allowed.</summary>
        private static string Describe(MethodBase target)
        {
            var owner = ShortName(target.DeclaringType);
            if (owner == null)
                return null;
            switch (target.Name)
            {
                case "GetAwaiter":
                    return $"{owner}.GetAwaiter() (await without a token)";
                case "ToUniTask" when target.GetParameters().Length == 0:
                    return $"{owner}.ToUniTask() (no token)";
                case "op_Implicit" when target is MethodInfo method && method.ReturnType.FullName == UNITASK:
                    return $"the conversion of a {owner} to UniTask (no token)";
                case "ToYieldInstruction":
                    return $"{owner}.ToYieldInstruction() (a coroutine wait without a token)";
                default:
                    return null;
            }
        }

        /// <summary>
        /// A tween converted to an interface or object. Mostly a wait without a token: <c>yield return tween</c>, or UniTask's
        /// <c>IEnumerator.WithCancellation</c>, which throws on cancel but leaves the tween running. Passing it as an object
        /// (<c>string.Format</c>) is reported too; mark such code exempt.
        /// </summary>
        private static string DescribeBox(Type boxed)
        {
            var name = ShortName(boxed);
            return name == null ? null : $"a {name} as an IEnumerator or object (a coroutine wait, IEnumerator.WithCancellation, or boxing)";
        }

        private static string ShortName(Type type) =>
            type?.FullName switch
            {
                TWEEN => "Tween",
                SEQUENCE => "Sequence",
                _ => null,
            };
    }
}
