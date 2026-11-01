using System;

namespace AF.Core
{
    /// <summary>
    /// Allows code to copy GameObjects or components with <c>Object.Instantiate</c>, or to add a component with [Inject]
    /// members or by <c>Type</c>, without going through <c>IObjectResolver</c> or a pool. Validation reports such calls anywhere else, because
    /// nothing injects the copy. On a type it covers every member, nested types and lambdas included.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
    public sealed class InstantiateExemptAttribute : Attribute
    {
        public InstantiateExemptAttribute(string reason) => Reason = reason;

        public string Reason { get; }
    }
}
