using System;

namespace AF.Core
{
    /// <summary>
    /// Allows code to await a PrimeTween tween or sequence some other way than <c>ToUniTask(cancellation)</c>. Validation
    /// reports such awaits anywhere else. On a type it covers every member, nested types and lambdas included.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
    public sealed class TweenAwaitExemptAttribute : Attribute
    {
        public TweenAwaitExemptAttribute(string reason) => Reason = reason;

        public string Reason { get; }
    }
}
