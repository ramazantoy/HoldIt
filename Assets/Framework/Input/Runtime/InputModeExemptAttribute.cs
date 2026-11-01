using System;

namespace AF.Input
{
    /// <summary>
    /// Allows code to enable or disable input actions, or to write <c>UnityEngine.Cursor</c>, itself: for example a debug
    /// console with its own standalone action that is not part of the actions asset. Validation reports such calls anywhere
    /// else. On a type it covers every member, nested types and lambdas included.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
    public sealed class InputModeExemptAttribute : Attribute
    {
        public InputModeExemptAttribute(string reason) => Reason = reason;

        public string Reason { get; }
    }
}
